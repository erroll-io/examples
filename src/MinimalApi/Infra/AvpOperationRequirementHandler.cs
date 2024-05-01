using System;
using System.Collections.Generic;
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
    private readonly AvpConfig _avpConfig;

    public AvpOperationRequirementHandler(
        IAmazonVerifiedPermissions avpClient,
        IOptions<AvpConfig> avpConfigOptions)
    {
        _avpClient = avpClient;
        _avpConfig = avpConfigOptions.Value;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationRequirement requirement)
    {
        (var conditionType, var conditionValue) = AvpUserRoleService.SplitCondition(requirement.Condition);

        var isAuthorizedResponse = await _avpClient.IsAuthorizedAsync(
            new IsAuthorizedRequest()
            {
                PolicyStoreId = _avpConfig.PolicyStoreId,
                Principal = new EntityIdentifier()
                {
                    EntityType = "MinimalApi::User",
                    EntityId = context.User.GetPrincipalIdentity()
                },
                Action = new ActionIdentifier
                {
                    ActionType = "MinimalApi::Action",
                    ActionId = requirement.Operation.Substring(requirement.Operation.LastIndexOf("::") + 2)
                },
                Resource = new EntityIdentifier()
                {
                    EntityType = conditionType,
                    //EntityType = $"MinimalApi::{conditionType}",
                    EntityId = conditionValue
                }
            });

        if (isAuthorizedResponse.Decision == Decision.ALLOW)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }
    }
}
