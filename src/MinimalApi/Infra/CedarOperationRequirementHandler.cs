using System;
using System.Linq;
using System.Threading.Tasks;

using Amazon.VerifiedPermissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using MinimalApi.CedarSharp;
using MinimalApi.Services;

namespace MinimalApi;

public class CedarOperationRequirementHandler : AuthorizationHandler<OperationRequirement>
{
    private readonly IUserRoleService _userRoleService;
    private readonly AuthConfig _authConfig;
    private readonly AvpConfig _avpConfig;

    public CedarOperationRequirementHandler(
        IUserRoleService userRoleService,
        IOptions<AuthConfig> authConfigOptionsSnapshot,
        IOptions<AvpConfig> avpConfigOptions)
    {
        _userRoleService = userRoleService;
        _authConfig = authConfigOptionsSnapshot.Value;
        _avpConfig = avpConfigOptions.Value;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationRequirement requirement)
    {
        if (!_authConfig.DoUseAvp || !_authConfig.DoUseCedar)
            return;

        var userRoles = await _userRoleService.GetUserRolesByUserId(context.User.GetPrincipalIdentity());

        var result = CedarsharpMethods.Authorize(
            userRoles
                .Select(userRole => new CedarSharp.AvpPolicy(userRole.Id, userRole.Metadata))
                .ToList(),
            $"MinimalApi::User::\"{context.User.GetPrincipalIdentity()}\"",
            requirement.Operation,
            requirement.Condition,
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
