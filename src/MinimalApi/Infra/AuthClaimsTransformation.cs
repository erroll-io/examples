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
    private readonly IUserRoleService _userRoleService;
    private readonly IRoleService _roleService;

    private bool _isHandled = false;

    public AuthClaimsTransformation(
        ILogger<AuthClaimsTransformation> logger,
        IUserService userService,
        IUserRoleService userRoleService,
        IRoleService roleService)
    {
        _logger = logger;
        _userService = userService;
        _userRoleService = userRoleService;
        _roleService = roleService;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity == default || !principal.Identity.IsAuthenticated || _isHandled)
        {
            return principal;
        }

        var principalClaims = (await GetUserClaims(principal)).ToList();

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

    private async Task<IEnumerable<Claim>> GetUserClaims(ClaimsPrincipal principal)
    {
        // TODO: claims caching

        var userResult = await _userService.GetCurrentUser(principal);

        var claims = new List<Claim>()
        {
            new Claim("username", userResult.Result.Id)
        };

        // TODO: improve this
        if (_userRoleService is AvpUserRoleService)
            return claims;

        var userRoles = await _userRoleService.GetUserRolesByUserId(userResult.Result.Id);

        var tasks = userRoles.Select(async (userRole) => 
        {
            var rolePermissions = await _roleService.GetRolePermissions(userRole.RoleId);

            return rolePermissions.Select(permission =>
                new Claim("permission", $"{permission.PermissionId}:{userRole.Condition}"));
        });

        var results = await Task.WhenAll(tasks);
        
        claims.AddRange(results.SelectMany(r => r));

        return claims.ToList();
    }
}
