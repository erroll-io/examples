using Erroll.CedarSharp;

namespace MinimalApi;

public class AuthorizerResult
{
    public AuthorizerDecision Decision { get; set; }
}

public enum AuthorizerDecision
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
        var result = CedarsharpMethods.Authorize(policy, principal, action, resource, context ?? "", entities ?? "");

        return new AuthorizerResult()
        {
            Decision = result == Decision.Allow
                ? AuthorizerDecision.Allow
                : AuthorizerDecision.Deny
        };
    }
}
