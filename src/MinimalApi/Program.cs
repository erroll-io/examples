using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using MinimalApi;

var graph4 = new Graph() { Id = "Four" };
graph4.AddEdges("5", new Edge("11"));
graph4.AddEdges("7", new Edge("11"), new Edge("8"));
graph4.AddEdges("3", new Edge("8"), new Edge("10"));
graph4.AddEdges("11", new Edge("2"), new Edge("9"), new Edge("10"));
graph4.AddEdges("8", new Edge("9"));

var graph5 = new Graph() { Id = "Five" };
graph5.AddEdges("5", new Edge("0"), new Edge("2"));
graph5.AddEdges("4", new Edge("0"), new Edge("1"));
graph5.AddEdges("2", new Edge("3"));
//graph5.AddEdges("3", new Edge("1"));

var foo = graph5.TopologicalSort();
var bar = graph5.Kahn();

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
