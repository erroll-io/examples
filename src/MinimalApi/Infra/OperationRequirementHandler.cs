using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using MinimalApi.Services;

namespace MinimalApi;

public class OperationRequirementHandler : AuthorizationHandler<OperationRequirement>
{
    private readonly AuthConfig _authConfig;

    public OperationRequirementHandler(IOptionsSnapshot<AuthConfig> authConfigOptionsSnapshot)
    {
        _authConfig = authConfigOptionsSnapshot.Value;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationRequirement requirement)
    {
        if (_authConfig.DoUseAvp)
            return Task.CompletedTask;

        if (context.User.HasPermission(requirement.Operation, requirement.Condition))
        {
            context.Succeed(requirement);
        }
        else
        {
            Console.WriteLine("Failing user: " + (string.Join(", ", context.User.Claims.Select(p => $"{p.Type}:{p.Value}"))));
        }

        return Task.CompletedTask;
    }
}
