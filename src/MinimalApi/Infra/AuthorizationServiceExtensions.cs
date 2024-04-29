using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace MinimalApi;

public static class AuthorizationServiceExtensions
{
    public static Task<AuthorizationResult> AuthorizeAsync(
        this IAuthorizationService service,
        ClaimsPrincipal user,
        params IAuthorizationRequirement[] requirements)
    {
        return service.AuthorizeAsync(user, default, requirements);
    }

    public static Task<AuthorizationResult> AuthorizeAsync(
        this IAuthorizationService service,
        ClaimsPrincipal user,
        string policyName)
    {
        return service.AuthorizeAsync(user, default, policyName);
    }
}
