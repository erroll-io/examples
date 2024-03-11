using System;
using System.IO;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.SimpleSystemsManagement;

using MinimalApi;
using MinimalApi.Services;

#if DEBUG
using Microsoft.AspNetCore.Hosting;
#endif

var builder = WebApplication.CreateSlimBuilder(args);

builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile(Path.Combine(builder.Environment.ContentRootPath, "appsettings.json"), false)
    .AddJsonFile(
        Path.Combine(builder.Environment.ContentRootPath, $"appsettings.{builder.Environment.EnvironmentName}.json"),
        true);

builder.Configuration.AddSystemsManagerWithHack("/minimal-api");
//builder.Configuration.AddSystemsManager("/minimal-api");

builder.Services.Configure<AwsConfig>(builder.Configuration.GetSection<AwsConfig>());
builder.Services.Configure<OauthConfig>(builder.Configuration.GetSection<OauthConfig>());
builder.Services.Configure<CryptoConfig>(builder.Configuration.GetSection<CryptoConfig>());
builder.Services.Configure<DynamoConfig>(builder.Configuration.GetSection<DynamoConfig>());

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Add(MinimalApiJsonSerializerContext.Default);
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

builder.Services.AddAWSService<IAmazonDynamoDB>();

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddSingleton<IHasher, Hasher>();

builder.Services.AddScoped<IUsersService, UsersService>();

#if DEBUG
Console.WriteLine("Registering Kestrel for local debugging.");
builder.WebHost.ConfigureKestrel(context => 
{
    context.ListenAnyIP(5055, opts =>
    {
        opts.UseHttps();
    });
});
#else
builder.Services.AddAWSLambdaHosting(
    LambdaEventSource.HttpApi,
    opts =>
    {
        opts.Serializer = new SourceGeneratorLambdaJsonSerializer<MinimalApiJsonSerializerContext>();
    });
#endif

var app = builder
    .Build();

app.MapGet("/health", HealthEndpoints.GetHealth);
app.MapPost("/health", HealthEndpoints.PostHealth);

app.MapPost("/users", UsersEndpoints.CreateUser);
app.MapGet("/users/{id}", UsersEndpoints.GetUser);
app.MapGet("/users/current", UsersEndpoints.GetCurrentUser);
app.MapPut("/users/{id}", UsersEndpoints.UpdateUser);
app.MapPut("/users/current", UsersEndpoints.UpdateCurrentUser);

await app.RunAsync();
