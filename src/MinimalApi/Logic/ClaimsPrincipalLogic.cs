using System.Linq;
using System.Security.Claims;

namespace MinimalApi;

public enum Provider
{
    Cognito,
    Google,
    Github
}

public static class ClaimsPrincipalLogic
{
    public static string GetPrincipalIdentity(this ClaimsPrincipal user)
    {
        var claim = user.Claims.FirstOrDefault(claim => claim.Type == "username");

        if (claim == default)
            claim = user.Claims.FirstOrDefault(claim => claim.Type == "sub");

        return claim.Value;
    }

    public static ClaimsPrincipal GetFakeUser(string username = default, string sub = default)
    {
        return new ClaimsPrincipal(
            new ClaimsIdentity(
                new []
                {
                    string.IsNullOrEmpty(username)
                        ? new Claim("sub", sub)
                        : new Claim("username", username)
                }));
    }
}