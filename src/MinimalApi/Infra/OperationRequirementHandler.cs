using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using MinimalApi.Services;

namespace MinimalApi;

public class OperationRequirementHandler : AuthorizationHandler<OperationRequirement>
{
    public OperationRequirementHandler()
    {
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationRequirement requirement)
    {
        if (!string.IsNullOrEmpty(requirement.Strategy) && requirement.Strategy != "CLAIMS")
            return Task.CompletedTask;

        if (context.User.HasPermission(requirement.Operation, requirement.Condition))
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }

        return Task.CompletedTask;
    }
}
