using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace MinimalApi.Services;

public class CedarComparisonService
{
    private readonly IServiceProvider _serviceProvider;

    public CedarComparisonService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<IEnumerable<AuthorizationComparisonResult>> Compare(int? n)
    {
        if (!n.HasValue)
            n = 1000;

        var claimsPrincipalFactory = _serviceProvider.GetRequiredService<ClaimsPrincipalFactory>();

        var strategies = new []
        {
            "CLAIMS",
            "AVP",
            "CEDAR"
        };

        var principals = new []
        {
            await claimsPrincipalFactory.GetClaimsPrincipal("user-one"),
            await claimsPrincipalFactory.GetClaimsPrincipal("user-two"),
            await claimsPrincipalFactory.GetClaimsPrincipal("user-three")
        };

        var actions = new []
        {
            "MinimalApi::Action::\"FReadProject\"",
            "MinimalApi::Action::\"ReadProject\"",
            "MinimalApi::Action::\"CreateProject\"",
            "MinimalApi::Action::\"CreateProjectData\"",
            "MinimalApi::Action::\"CreateProjectResults\"",
            "MinimalApi::Action::\"DeleteProject\"",
            "MinimalApi::Action::\"DeleteProjectData\"",
            "MinimalApi::Action::\"DeleteProjectResults\"",
            "MinimalApi::Action::\"ReadProject\""
        };

        var resources = new []
        {
            "MinimalApi::Project::\"project-one\"",
            "MinimalApi::Project::\"project-two\"",
            "MinimalApi::Project::\"project-three\""
        };

        var random = new Random();
        Stopwatch stopwatch;

        var results = new List<AuthorizationComparisonItem>(n.Value);

        for (var i = 0; i < n.Value; i++)
        {
            var authorizationService = _serviceProvider.GetRequiredService<IAuthorizationService>();
            var strategy = strategies[random.Next(strategies.Length)];
            var principal = principals[random.Next(principals.Length)];
            var action = actions[random.Next(actions.Length)];
            var resource = resources[random.Next(resources.Length)];
            var requirement = new OperationRequirement(action, resource, strategy);

            stopwatch = Stopwatch.StartNew();
            var result = await authorizationService.AuthorizeAsync(principal, requirement);
            stopwatch.Stop();

            results.Add(
                new AuthorizationComparisonItem()
                {
                    Strategy = strategy,
                    Principal = principal.GetPrincipalIdentity(),
                    Action = action,
                    Resource = resource,
                    ElapsedUs = stopwatch.Elapsed.TotalMicroseconds
                });
        }

        var comparisonResults = new List<AuthorizationComparisonResult>(strategies.Length);

        foreach (var strategy in strategies)
        {
            var strategyResults = results
                .Where(result => result.Strategy == strategy)
                .Skip(1)
                .ToList();

            if (!strategyResults.Any())
                continue;

            comparisonResults.Add(new AuthorizationComparisonResult()
            {
                StrategyName = strategy,
                ExecutionCount = strategyResults.Count,
                ElapsedUsMin = strategyResults.Min(result => result.ElapsedUs),
                ElapsedUsMax = strategyResults.Max(result => result.ElapsedUs),
                ElapsedUsAverage = strategyResults.Average(result => result.ElapsedUs),
                ElapsedUsStdDev = strategyResults
                    .Select(result => result.ElapsedUs)
                    .StdDev(),
            });
        }

        return comparisonResults;
    }
}

public class AuthorizationComparisonItem
{
    public string Strategy { get; set; }
    public string Principal { get; set; }
    public string Action { get; set; }
    public string Resource { get; set; }
    public double ElapsedUs { get; set; }
}

public class AuthorizationComparisonResult
{
    public string StrategyName { get; set; }
    public int ExecutionCount { get; set; }
    public double ElapsedUsMin { get; set; }
    public double ElapsedUsMax { get; set; }
    public double ElapsedUsAverage { get; set; }
    public double ElapsedUsStdDev { get; set; }
}

internal static class Extensions
{
    public static double StdDev(this IEnumerable<double> values)
    {
        var count = values.Count();

        if (count <= 1)
            return 0;
    
        var avg = values.Average();

        var sum = values.Sum(p => (p - avg) * (p - avg));

        return Math.Sqrt(sum/count);
    }
}
