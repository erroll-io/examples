using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using MinimalApi.Services;

namespace MinimalApi;

public class OperationRequirementHandler : AuthorizationHandler<OperationRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationRequirement requirement)
    {
        if (context.User.HasPermission(requirement.Operation, requirement.Condition))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
