using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace MinimalApi.Services;

public static class ClaimsPrincipalLogic
{
    public static string GetPrincipalIdentity(this ClaimsPrincipal user)
    {
        return GetPrincipalIdentity(user, out var _);
    }

    public static string GetPrincipalIdentity(this ClaimsPrincipal principal, out string claimType)
    {
        // Cognito `sub` values are globally unique, thus we cannot rely on them
        // to persist after a data recovery event, so we use `principalname` instead.

        var claim = principal.Claims.FirstOrDefault(claim => claim.Type == "username");

        if (claim == default)
        {
            var sub = principal.GetSub();

            if (string.IsNullOrEmpty(sub))
            {
                claimType = string.Empty;

                return string.Empty;
            }

            claimType = "sub";
            return sub;
        }
        else
        {
            claimType = claim.Type;

            return claim.Value;
        }
    }

    public static string GetSub(this ClaimsPrincipal principal)
    {
        return principal.Claims.FirstOrDefault(claim => claim.Type == "sub")?.Value;
    }

    public static ClaimsIdentity CloneIdentity(this ClaimsPrincipal principal)
    {
        var identity = principal.Identity as ClaimsIdentity;

        return new ClaimsIdentity(
            identity.Claims,
            identity.AuthenticationType,
            identity.NameClaimType,
            identity.RoleClaimType);
    }

    public static bool HasPermission(
        this ClaimsPrincipal user,
        string permissionId,
        string condition)
    {
        return user.Claims
            .Any(claim => claim.Type == "permission"
                && claim.Value == $"{permissionId}:{condition}");
    }

    public static IEnumerable<string> GetResourceIdsForPermissionCondition(
        this ClaimsPrincipal user,
        string permissionId,
        string conditionKey)
    {
        return user.Claims
            .Where(claim => claim.Type == "permission"
                && claim.Value.StartsWith($"{permissionId}:{conditionKey}"))
            .Select(claim => claim.Value.Substring(claim.Value.LastIndexOf(":") + ":".Length));
    }
}
