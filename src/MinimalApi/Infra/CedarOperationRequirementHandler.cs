using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.VerifiedPermissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using MinimalApi.CedarSharp;
using MinimalApi.Services;

namespace MinimalApi;

public class CedarOperationRequirementHandler : AuthorizationHandler<OperationRequirement>
{
    private readonly IDistributedCache _cache;
    private readonly IUserRoleService _userRoleService;
    private readonly AvpConfig _avpConfig;

    public CedarOperationRequirementHandler(
        IDistributedCache cache,
        AvpUserRoleService userRoleService,
        IOptions<AvpConfig> avpConfigOptions)
    {
        _cache = cache;
        _userRoleService = userRoleService;
        _avpConfig = avpConfigOptions.Value;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationRequirement requirement)
    {
        if (requirement.Strategy != "CEDAR")
            return;
        
        var principalId = context.User.GetPrincipalIdentity();
        var cacheKey = $"CEDAR_POLICIES::{principalId}";

        var policies = await _cache.Get<List<CedarSharp.AvpPolicy>>(cacheKey);

        if (policies == default)
        {
            var userRoles = await _userRoleService.GetUserRolesByUserId(principalId);
            
            policies = userRoles
                .Select(userRole => new CedarSharp.AvpPolicy(userRole.Id, userRole.Metadata))
                .ToList();

            // TODO: expiry
            await _cache.Set(cacheKey, policies);
        }

        var result = CedarsharpMethods.Authorize(
            policies,
            $"MinimalApi::User::\"{principalId}\"",
            requirement.Operation,
            requirement.Condition ?? "MinimalApi::PlaceHolder::0",// TODO: afaict a value is required here, and "*" does _not_ work,
            "",
            "");

        if (result.result == CedarSharp.Decision.Allow)
        {
            context.Succeed(requirement);
        }
        else
        {
            // TODO: additional context

            context.Fail();
        }
    }
}
