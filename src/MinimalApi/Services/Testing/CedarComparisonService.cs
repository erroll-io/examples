using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MathNet.Numerics.Statistics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MinimalApi.CedarSharp;


namespace MinimalApi.Services;

public class CedarComparisonService
{
    private static string[] _users = new []
    {
        "user-one",
        "user-two",
        "user-three"
    };

    private static string[] _actions = new []
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

    private static string[] _resources = new []
    {
        "MinimalApi::Project::\"project-one\"",
        "MinimalApi::Project::\"project-two\"",
        "MinimalApi::Project::\"project-three\""
    };

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

        var principals = _users
            .Select(async user => await claimsPrincipalFactory.GetClaimsPrincipal(user))
            .Select(task => task.Result)
            .ToArray();

        var random = new Random();
        Stopwatch stopwatch;

        var results = new List<AuthorizationComparisonItem>(n.Value);

        for (var i = 0; i < n.Value; i++)
        {
            var authorizationService = _serviceProvider.GetRequiredService<IAuthorizationService>();
            var strategy = strategies[random.Next(strategies.Length)];
            var principal = principals[random.Next(principals.Length)];
            var action = _actions[random.Next(_actions.Length)];
            var resource = _resources[random.Next(_resources.Length)];
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

    public async Task<(double, double)> TestPolicySize(int? n)
    {
        if (!n.HasValue)
            n = 1000;

        var results = new List<(int, double)>();

        for (var i = 0; i < n; i++)
        {
            var policiesLength = 0;

            var random = new Random();
            Stopwatch stopwatch;

            var policies = new List<CedarSharp.CedarPolicy>();

            for (var j = 0; j < random.Next(1, 10); j++)
            {
                (var policy, var policySize) = GetRandomPolicy(random.Next(20));

                policies.Add(policy);
                policiesLength += policySize;
            }

            stopwatch = Stopwatch.StartNew();
            CedarsharpMethods.Authorize(
                policies,
                $"MinimalApi::User::{_users[random.Next(_users.Length)]}",
                _actions[random.Next(_actions.Length)],
                _resources[random.Next(_resources.Length)],
                "",
                "");
            stopwatch.Stop();

            if (i > 0)
                results.Add((policiesLength, stopwatch.Elapsed.TotalMicroseconds));
        }

        var correlation = Correlation.Pearson(results.Select(p => (double) p.Item1), results.Select(p => p.Item2));
        var averageUsPerByte = results.Select(p => p.Item2/(p.Item1*sizeof(char))).Average();

        return (correlation, averageUsPerByte);
    }

    private (CedarSharp.CedarPolicy, int) GetRandomPolicy(int actionCount)
    {
        var random = new Random();

        var policy = "permit(principal in MinimalApi::User::\"{principalId}\", "
            + "action in [{actions}], "
            + "resource in {resource});";

        var actionBuilder = new StringBuilder();

        for (var i = 0; i < actionCount; i++)
        {
            if (i > 0)
                actionBuilder.Append(", ");

            actionBuilder.Append(_actions[random.Next(_actions.Length)]);
        }

        return (
            new CedarSharp.CedarPolicy(
                Guid.NewGuid().ToString(),
                policy
                    .Replace("{principalId}", _users[random.Next(_users.Length)])
                    .Replace("{actions}", actionBuilder.ToString())
                    .Replace("{resource}", _resources[random.Next(_resources.Length)])),
            policy.Length);
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
