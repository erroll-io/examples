using System;
using System.IO;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
#if DEBUG
using Microsoft.AspNetCore.Hosting;
#endif
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.SimpleSystemsManagement;

using MinimalApi;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile(Path.Combine(builder.Environment.ContentRootPath, "appsettings.json"), false);

builder.Configuration.AddSystemsManagerWithHack("/minimal-api");
//builder.Configuration.AddSystemsManager("/minimal-api");

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Add(MinimalApiJsonSerializerContext.Default);
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

builder.Services.AddAWSLambdaHosting(
    LambdaEventSource.HttpApi,
    opts =>
    {
        opts.Serializer = new SourceGeneratorLambdaJsonSerializer<MinimalApiJsonSerializerContext>();
    });

builder.Services.Configure<AwsConfig>(builder.Configuration.GetSection<AwsConfig>());
builder.Services.Configure<OauthConfig>(builder.Configuration.GetSection<OauthConfig>());
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

#if DEBUG
Console.WriteLine("Registering Kestrel for local debugging.");
builder.WebHost.ConfigureKestrel(context => 
{
    context.ListenAnyIP(5055, opts =>
    {
        opts.UseHttps();
    });
});
#endif

var app = builder
    .Build();

app.MapGet("/health", HealthController.GetHealth);
app.MapPost("/health", HealthController.PostHealth);

await app.RunAsync();
