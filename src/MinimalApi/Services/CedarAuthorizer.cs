using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.Authorization;
using MinimalApi.CedarSharp;

namespace MinimalApi;

public class CedarPolicy
{
    public string Id { get; set; }
    public string Policy { get; set; }

    public CedarPolicy()
    {
    }

    public CedarPolicy(string id, string policy)
    {
        Id = id;
        Policy = policy;
    }
}

public class CedarAuthorizer
{
    public AuthorizationResult Authorize(
        string policy,
        string principal,
        string action,
        string resource,
        string context = "",
        string entities = "")
    {
        return Authorize(
            new List<CedarPolicy>() { new CedarPolicy() { Id = string.Empty, Policy = policy } },
            principal,
            action,
            resource,
            context ?? "",
            entities ?? "");
    }

    public AuthorizationResult Authorize(
        IEnumerable<CedarPolicy> policies,
        string principal,
        string action,
        string resource,
        string context = "",
        string entities = "")
    {
        var result = CedarsharpMethods.Authorize(
            policies
                .Select(policy => new CedarSharp.CedarPolicy(policy.Id, policy.Policy))
                .ToList(),
            principal,
            action,
            resource,
            context ?? "",
            entities ?? "");

        return result.result == Decision.Allow
            ? AuthorizationResult.Success()
            : AuthorizationResult.Failed();
    }
}
