using System.Diagnostics;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using MinimalApi;

var app = WebApplication.CreateSlimBuilder(args)
    .ConfigureApplicationConfiguration(AppConstants.AppNameFqn)
    .ConfigureHostServices()
    .ConfigureApplicationServices()
    .Build()
    .ConfigureApplication()
    .RegisterEndpoints();


#if DEBUG
var seeder = app.Services.GetRequiredService<DynamoSeeder>();

await seeder.SeedData();
#endif

await app.RunAsync();
