using System;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
#if DEBUG
using Microsoft.AspNetCore.Hosting;
#endif
using Microsoft.Extensions.DependencyInjection;

using Amazon.Lambda.Serialization.SystemTextJson;

using MinimalApi;

var builder = WebApplication.CreateSlimBuilder(args);

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
