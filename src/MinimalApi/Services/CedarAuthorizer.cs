using Microsoft.AspNetCore.Authorization;
using MinimalApi.CedarSharp;

namespace MinimalApi;

public interface IAuthorizer
{
    AuthorizationResult Authorize(
        string policy,
        string principal,
        string action,
        string resource,
        string context = "",
        string entities = "");
}

public class CedarAuthorizer : IAuthorizer
{
    public AuthorizationResult Authorize(
        string policy,
        string principal,
        string action,
        string resource,
        string context = "",
        string entities = "")
    {
        var result = CedarsharpMethods.Authorize(policy, principal, action, resource, context ?? "", entities ?? "");

        return result == Decision.Allow
            ? AuthorizationResult.Success()
            : AuthorizationResult.Failed();
    }
}
