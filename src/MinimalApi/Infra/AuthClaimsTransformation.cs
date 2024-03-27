using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;


using MinimalApi.Services;

namespace MinimalApi;

public class AuthClaimsTransformation : IClaimsTransformation
{
    private readonly ILogger _logger;
    private readonly IUserService _userService;
    private readonly IAuthClaimsService _authClaimsService;

    private bool _isHandled = false;

    public AuthClaimsTransformation(
        ILogger<AuthClaimsTransformation> logger,
        IUserService userService,
        IAuthClaimsService authClaimsService)
    {
        _logger = logger;
        _userService = userService;
        _authClaimsService = authClaimsService;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity == default || !principal.Identity.IsAuthenticated || _isHandled)
        {
            return principal;
        }

        // we'll grab claims from our own store, but they could alternatively
        // be coming from a different service (our own, or third-party)
        var principalClaims = (await _authClaimsService.GetUserClaims(principal)).ToList();

        if (principalClaims != default && principalClaims.Any())
        {
            var identity = principal.CloneIdentity();

            identity.AddClaims(principalClaims);

            _isHandled = true;
            return new ClaimsPrincipal(identity);
        }

        _isHandled = true;

        return principal;
    }
}
