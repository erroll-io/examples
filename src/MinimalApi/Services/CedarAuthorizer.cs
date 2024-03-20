using Erroll.CedarSharp;

namespace MinimalApi;

public enum AuthorizerResult
{
    Deny,
    Allow
}

public interface IAuthorizer
{
    AuthorizerResult Authorize(
        string policy,
        string principal,
        string action,
        string resource,
        string context = "",
        string entities = "");
}

public class CedarAuthorizer : IAuthorizer
{
    public AuthorizerResult Authorize(
        string policy,
        string principal,
        string action,
        string resource,
        string context = "",
        string entities = "")
    {
        return CedarsharpMethods.Authorize(policy, principal, action, resource, context, entities) == "ALLOW"
            ? AuthorizerResult.Allow
            : AuthorizerResult.Deny;
    }
}
