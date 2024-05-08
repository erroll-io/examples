using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;

using Amazon.VerifiedPermissions.Model;

namespace MinimalApi.Services;

public static class AvpLogic
{
    public static EntityReference ToPrincipalReference(this ClaimsPrincipal principal)
    {
        return ToPrincipalReference(principal.GetPrincipalIdentity());
    }

    public static EntityReference ToPrincipalReference(string principalId)
    {
        return new EntityReference()
        {
            Identifier = ToPrincipalEntity(principalId)
        };
    }

    public static EntityIdentifier ToPrincipalEntity(this ClaimsPrincipal principal)
    {
        return ToPrincipalEntity(principal.GetPrincipalIdentity());
    }

    public static EntityIdentifier ToPrincipalEntity(string principalId)
    {
        return new EntityIdentifier()
        {
            EntityType = "MinimalApi::User",
            EntityId = principalId
        };
    }

    public static ActionIdentifier ToActionEntity(this OperationRequirement requirement)
    {
        return new ActionIdentifier
        {
            ActionType = "MinimalApi::Action",
            ActionId = requirement.Operation.Substring(requirement.Operation.LastIndexOf("::") + 2)
        };
    }

    public static EntityReference ToResourceReference(string condition)
    {
        return new EntityReference()
        {
            Identifier = ToResourceEntity(condition)
        };
    }

    public static EntityIdentifier ToResourceEntity(this OperationRequirement requirement)
    {
        return ToResourceEntity(requirement.Condition);
    }

    public static EntityIdentifier ToResourceEntity(string condition)
    {
        (var conditionType, var conditionValue) = SplitCondition(condition);

        return new EntityIdentifier()
        {
            EntityType = conditionType,
            EntityId = conditionValue
        };
    }

    public static (string, string) SplitCondition(string condition)
    {
        var match = Regex.Match(condition, @"\w+(:)\w+");

        if (!match.Success)
            throw new Exception("Invalid condition.");

        return (condition.Substring(0, match.Groups[1].Index), condition.Substring(match.Groups[1].Index + 1));
    }

    private static Regex _permitsRegex =
        new Regex(@"permit\s?\((.*)\)", RegexOptions.Compiled | RegexOptions.Singleline);
    private static Regex _actionsRegex =
        new Regex(@"action in \[(.*)\]", RegexOptions.Compiled | RegexOptions.Singleline);

    public static Dictionary<string, List<string>> ParsePolicyTemplateActions(
        IEnumerable<GetPolicyTemplateResponse> policyTemplates)
    {
        var dict = new Dictionary<string, List<string>>();

        foreach (var template in policyTemplates)
        {
            var permitsMatch = _permitsRegex.Match(template.Statement);

            if (!permitsMatch.Success)
                continue;
            
            var actionsMatch = _actionsRegex.Match(permitsMatch.Groups[1].Value);

            if (!actionsMatch.Success)
                continue;
            
            foreach (var action in actionsMatch.Groups[1].Value.Split(',')
                .Select(p => p.Trim().Replace("\"", "")))
            {
                if (!dict.ContainsKey(action))
                    dict[action] = new List<string>();

                dict[action].Add(template.PolicyTemplateId);
            }
        }

        return dict;
    }
}
