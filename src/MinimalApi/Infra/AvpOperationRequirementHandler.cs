using System;
using System.Threading.Tasks;

using Amazon.VerifiedPermissions;
using Amazon.VerifiedPermissions.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using MinimalApi.Services;

namespace MinimalApi;

public class AvpOperationRequirementHandler : AuthorizationHandler<OperationRequirement>
{
    private readonly IAmazonVerifiedPermissions _avpClient;
    private readonly AuthConfig _authConfig;
    private readonly AvpConfig _avpConfig;

    public AvpOperationRequirementHandler(
        IAmazonVerifiedPermissions avpClient,
        IOptions<AuthConfig> authConfigOptionsSnapshot,
        IOptions<AvpConfig> avpConfigOptions)
    {
        _avpClient = avpClient;
        _authConfig = authConfigOptionsSnapshot.Value;
        _avpConfig = avpConfigOptions.Value;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationRequirement requirement)
    {
        if (!_authConfig.DoUseAvp || _authConfig.DoUseCedar)
            return;

        var isAuthorizedResponse = await _avpClient.IsAuthorizedAsync(
            new IsAuthorizedRequest()
            {
                PolicyStoreId = _avpConfig.PolicyStoreId,
                Principal = context.User.ToPrincipalEntity(),
                Action = requirement.ToActionEntity(),
                Resource = requirement.ToResourceEntity()
            });

        if (isAuthorizedResponse.Decision == Decision.ALLOW)
        {
            context.Succeed(requirement);
        }
        else
        {
            // TODO: context

            context.Fail();
        }
    }
}
