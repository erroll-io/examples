using System;
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
}