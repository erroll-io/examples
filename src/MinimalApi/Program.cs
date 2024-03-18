using Microsoft.AspNetCore.Builder;

using MinimalApi;

await WebApplication.CreateSlimBuilder(args)
    .ConfigureApplicationConfiguration("/minimal-api")
    .ConfigureHostServices()
    .ConfigureApplicationServices()
    .Build()
    .ConfigureApplication()
    .RegisterEndpoints()
    .RunAsync();
