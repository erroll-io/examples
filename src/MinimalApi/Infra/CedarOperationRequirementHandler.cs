using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using MinimalApi.Services;

using static MinimalApi.CedarSharp.CedarsharpMethods;

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

        var result = Authorize(
            policies,
            $"MinimalApi::User::\"{principalId}\"",
            requirement.Operation,
            // TODO: afaict a value is required here, and "*" does _not_ work:
            requirement.Condition ?? "MinimalApi::PlaceHolder::0",
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
