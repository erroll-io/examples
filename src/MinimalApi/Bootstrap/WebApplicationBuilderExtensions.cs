using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Text.Json;

using Amazon.DynamoDBv2;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.VerifiedPermissions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;

using MinimalApi.Services;

namespace MinimalApi;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder ConfigureApplicationConfiguration(
        this WebApplicationBuilder builder,
        string appName)
    {
        builder.Configuration
            .SetBasePath(builder.Environment.ContentRootPath)
            .AddJsonFile(
                Path.Combine(
                    builder.Environment.ContentRootPath,
                    "appsettings.json"),
                false)
            .AddJsonFile(
                Path.Combine(
                    builder.Environment.ContentRootPath,
                    $"appsettings.{builder.Environment.EnvironmentName}.json"),
                true);

        builder.Configuration.AddSystemsManagerWithHack(appName);
        //builder.Configuration.AddSystemsManager("/minimal-api");

        builder.Services.Configure<AwsConfig>(builder.Configuration.GetSection<AwsConfig>());
        builder.Services.Configure<CryptoConfig>(builder.Configuration.GetSection<CryptoConfig>());
        builder.Services.Configure<DynamoConfig>(builder.Configuration.GetSection<DynamoConfig>());
        builder.Services.Configure<GoogleConfig>(builder.Configuration.GetSection<GoogleConfig>());
        builder.Services.Configure<OAuthConfig>(builder.Configuration.GetSection<OAuthConfig>());
        builder.Services.Configure<AvpConfig>(builder.Configuration.GetSection<AvpConfig>());

        return builder;
    }

    public static WebApplicationBuilder ConfigureHostServices(this WebApplicationBuilder builder)
    {
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Add(MinimalApiJsonSerializerContext.Default);
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.SerializerOptions.PropertyNameCaseInsensitive = true;
        });

        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
        builder.Services
            .AddAuthentication(options => 
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme);
        builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<GoogleConfig>, IOptions<OAuthConfig>>(
                (options, googleConfigOptions, oAuthConfigOptions) =>
                {
#if DEBUG
                    IdentityModelEventSource.ShowPII = true;
                    IdentityModelEventSource.LogCompleteSecurityArtifact = true;

#endif
                    options.Authority = oAuthConfigOptions.Value.AuthorityUrl;
                    options.MapInboundClaims = false;
                    options.TokenValidationParameters = new TokenValidationParameters()
                    {
                        ValidateIssuerSigningKey = true,
                        ValidateAudience = true,
                        ValidAudience = googleConfigOptions.Value.PortalClientId,
                        ValidateIssuer = true,
                        ValidIssuer = oAuthConfigOptions.Value.AuthorityUrl,
                        ValidateLifetime = true
                    };
                });
        builder.Services.AddAuthorization();

        builder.Services.AddScoped<IAuthorizationService, TracingDefaultAuthorizationService>();
        builder.Services.AddScoped<IAuthorizationHandler, OperationRequirementHandler>();
        builder.Services.AddScoped<IAuthorizationHandler, AvpOperationRequirementHandler>();
        //builder.Services.AddTransient<IAuthorizer, CedarAuthorizer>();

        builder.Services.AddCors();
        builder.Services.AddOptions<CorsOptions>()
            .Configure<IOptions<OAuthConfig>>((options, oAuthConfigOptions) =>
            {
                options.AddDefaultPolicy(
                    builder =>
                    {
                        builder.AllowAnyMethod();
                        builder.AllowAnyHeader();
                        builder.AllowCredentials();
                        builder.WithOrigins(oAuthConfigOptions.Value.AllowedOrigins.ToArray());
                    });
            });

        builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        builder.Services.AddTransient<IClaimsTransformation, AuthClaimsTransformation>();

        if (builder.Environment.IsEnvironment("Local"))
        {
            Console.WriteLine("Registering Kestrel for local debugging.");
            builder.WebHost.ConfigureKestrel(context => 
            {
                context.ListenAnyIP(5055, opts =>
                {
                    opts.UseHttps();
                });
            });
        }
        else
        {
            builder.Services.AddAWSLambdaHosting(
                LambdaEventSource.HttpApi,
                opts =>
                {
                    opts.Serializer = new SourceGeneratorLambdaJsonSerializer<MinimalApiJsonSerializerContext>();
                });
        }

#if DEBUG
        builder.Services.AddSingleton<DynamoSeeder>();
#endif

        return builder;
    }

    public static WebApplicationBuilder ConfigureApplicationServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddTransient<IAmazonDynamoDB, AmazonDynamoDBClient>();
        builder.Services.AddTransient<IAmazonVerifiedPermissions, AmazonVerifiedPermissionsClient>();
        // TODO: this ends up calling the same broken client factory that we
        // hacked around for the SSM configuration extension.
        //builder.Services.AddAWSService<IAmazonDynamoDB>();

        builder.Services.AddSingleton<IHasher, Hasher>();
        builder.Services.AddScoped<IUserService, UserService>();
        builder.Services.AddScoped<IProjectService, ProjectService>();
        builder.Services.AddScoped<IPermissionService, PermissionService>();
        builder.Services.AddScoped<IRoleService, RoleService>();
        builder.Services.AddScoped<IDataService, DataService>();

        builder.Services.AddScoped<UserRoleService>();
        builder.Services.AddScoped<AvpUserRoleService>();

        builder.Services.AddTransient<IOptions<AuthConfig>>(provider =>
        {
            StringValues doUseAvpHeader = string.Empty;
            provider.GetRequiredService<IHttpContextAccessor>()?.HttpContext?
                .Request.Headers.TryGetValue("X-MINIMAL-API-USE-AVP", out doUseAvpHeader);

            return new Options<AuthConfig>(
                new AuthConfig()
                {
                    DoUseAvp = string.IsNullOrEmpty(doUseAvpHeader) ? false : bool.Parse(doUseAvpHeader)
                });
        });

        builder.Services.AddScoped<IUserRoleService>(provider =>
        {
            return provider.GetRequiredService<IOptions<AuthConfig>>().Value.DoUseAvp
                ? provider.GetRequiredService<AvpUserRoleService>()
                : provider.GetRequiredService<UserRoleService>();
        });

        //builder.Services.AddDistributedMemoryCache();
        return builder;
    }
}

public class Options<T> : IOptions<T>
    where T : class
{
    private readonly T _value;

    public Options(T value)
    {
        _value = value;
    }

    public T Value => _value;
}
