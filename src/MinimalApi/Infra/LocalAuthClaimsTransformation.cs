using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;


using MinimalApi.Services;

namespace MinimalApi;

public class LocalAuthClaimsTransformation : IClaimsTransformation
{
    private readonly ILogger _logger;
    private readonly IUserService _userService;
    private readonly IRoleService _roleService;
    private readonly IUserRoleService _userRoleService;

    private bool _isHandled = false;

    public LocalAuthClaimsTransformation(
        ILogger<LocalAuthClaimsTransformation> logger,
        IUserService userService,
        IRoleService roleService,
        IUserRoleService userRoleService)
    {
        _logger = logger;
        _userService = userService;
        _roleService = roleService;
        _userRoleService = userRoleService;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity == default || !principal.Identity.IsAuthenticated || _isHandled)
        {
            return principal;
        }

        // we'll grab claims from our own store, but they could alternatively
        // be coming from a different service (our own, or third-party)
        var principalClaims = (await GetLocalClaims(principal)).ToList();

        // TODO: claims caching

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

    private async Task<IEnumerable<Claim>> GetLocalClaims(ClaimsPrincipal principal)
    {
        var user = await _userService.GetCurrentUser(principal);

        var userRoles = await _userRoleService.GetUserRolesByUserId(user.Id);

        var tasks = userRoles.Select(async (userRole) => 
        {
            var rolePermissions = await _roleService.GetRolePermissions(userRole.RoleId);

            return rolePermissions.Select(permission =>
                new Claim("permission", $"{permission.PermissionId}:{userRole.Condition}"));
        });

        var results = await Task.WhenAll(tasks);

        return results.SelectMany(r => r);
    }
}
