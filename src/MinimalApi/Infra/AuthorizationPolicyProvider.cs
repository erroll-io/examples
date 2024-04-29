using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace MinimalApi;

public class AuthorizationPolicyProvider : DefaultAuthorizationPolicyProvider
{
    public AuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
        : base(options)
    {
    } 

    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // let's assume policy names like:
        // "MinimalApi::Action::GetProject:Project:42"

        if (!policyName.StartsWith("MinimalApi::"))
            return base.GetPolicyAsync(policyName);

        var policy = new AuthorizationPolicyBuilder("Bearer");

        var actionConditionSplits = new System.Text.RegularExpressions.Regex("(?<!:):(?!:)").Split(policyName);
        var action = actionConditionSplits[0];
        var condition = actionConditionSplits[1];

        policy.AddRequirements(new OperationRequirement(action, condition));

        return Task.FromResult(policy.Build());
    }
}

