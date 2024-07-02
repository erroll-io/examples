using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        var result = CedarsharpMethods.Authorize(
            await GetPolicies(principalId),
            $"MinimalApi::User::\"{principalId}\"",
            requirement.Operation,
            // TODO: afaict a value is required here, and "*" does _not_ work:
            requirement.Condition ?? "MinimalApi::PlaceHolder::\"0\"",
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

    private async Task<List<CedarSharp.CedarPolicy>> GetPolicies(string principalId)
    {
        // TODO: this needs further consideration. For now just get/cache all of the
        // AVP Policies associated with the principal.

        var cacheKey = $"authz_policies::{principalId}";

        var policyIds = await _cache.Get<string[]>(cacheKey);

        if (policyIds == default)
        {
            var userRoles = await _userRoleService.GetUserRolesByUserId(principalId);
            
            policyIds = userRoles
                .Select(userRole => userRole.Id)
                .ToArray();

            await _cache.Set(cacheKey, policyIds);
        }

        return policyIds
            .Select(policyId =>
                new CedarSharp.CedarPolicy(
                    policyId,
                    AvpUserRoleService.AvpLookup.PolicyTemplateStatementsByPolicyTemplateId[policyId]))
            .ToList();
    }
}
