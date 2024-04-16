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
        IEnumerable<IAuthorizationRequirement> requirements)
    {
        return service.AuthorizeAsync(user, default, requirements);
    }

    public static Task<AuthorizationResult> AuthorizeAsync(
        this IAuthorizationService service,
        ClaimsPrincipal user,
        IAuthorizationRequirement requirement)
    {
        return service.AuthorizeAsync(user, default, requirement);
    }
}
