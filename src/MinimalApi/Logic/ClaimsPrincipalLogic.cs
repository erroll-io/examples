using System.Linq;
using System.Security.Claims;

namespace MinimalApi;

public static class ClaimsPrincipalLogic
{
    public static string GetPrincipalIdentity(this ClaimsPrincipal user)
    {
        // Cognito `sub` values are globally unique, thus we cannot rely on them
        // to persist after a data recovery event, so we use `username` instead.

        var claim = user.Claims.FirstOrDefault(claim => claim.Type == "username");

        if (claim == default)
            claim = user.Claims.FirstOrDefault(claim => claim.Type == "sub");

        return claim?.Value;
    }
}
