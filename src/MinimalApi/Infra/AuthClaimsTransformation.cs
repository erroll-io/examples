using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MinimalApi.Services;

namespace MinimalApi;

public class AuthClaimsTransformation : IClaimsTransformation
{
    private readonly ILogger _logger;
    private readonly IDistributedCache _cache;
    private readonly IUserService _userService;
    private readonly IUserRoleService _userRoleService;
    private readonly IRoleService _roleService;
    private readonly AuthConfig _authConfig;

    private bool _isHandled = false;

    public AuthClaimsTransformation(
        ILogger<AuthClaimsTransformation> logger,
        IUserService userService,
        IUserRoleService userRoleService,
        IRoleService roleService,
        IOptions<AuthConfig> authConfigOptions,
        IDistributedCache cache = default)
    {
        _logger = logger;
        _userService = userService;
        _userRoleService = userRoleService;
        _roleService = roleService;
        _authConfig = authConfigOptions.Value;
        _cache = cache;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var sw = new Stopwatch();

        sw.Start();
        if (principal.Identity != default && principal.Identity.IsAuthenticated && !_isHandled)
        {
            var principalClaims = (await GetUserClaims(principal)).ToList();

            if (principalClaims != default && principalClaims.Any())
            {
                var identity = principal.CloneIdentity();

                identity.AddClaims(principalClaims.Select(claim => new Claim(claim.Type, claim.Value)));

                principal = new ClaimsPrincipal(identity);
            }

            _isHandled = true;
        }
        sw.Stop();

        _logger.LogTrace($"MinimalApi::Metric::{(_authConfig.DoUseAvp ? "AVP" : "")}ClaimsTxEvalTimeMs: {sw.ElapsedMilliseconds}");

        return principal;
    }

    private async Task<IEnumerable<ClaimLite>> GetUserClaims(ClaimsPrincipal principal)
    {
        var userId = await _userService.GetCurrentUserId(principal);

        var claims = _cache == default ? null : await _cache.Get<List<ClaimLite>>(userId);

        if (claims != default && claims.Any())
        {
            _logger.LogInformation($"Using cached claims for {userId}.");
            return claims;
        }

        claims = new List<ClaimLite>()
        {
            new ClaimLite("username", userId)
        };

        if (!_authConfig.DoUseAvp)
        {
            var userRoles = await _userRoleService.GetUserRolesByUserId(userId);

            var tasks = userRoles.Select(async (userRole) => 
            {
                var rolePermissions = await _roleService.GetRolePermissions(userRole.RoleId);

                return rolePermissions.Select(permission =>
                    new ClaimLite("permission", $"{permission.PermissionId}:{userRole.Condition}"));
            });

            var results = await Task.WhenAll(tasks);
            
            claims.AddRange(results.SelectMany(r => r));
        }
        
        if (_cache != default)
            await _cache.Set(userId, claims);

        return claims.ToList();
    }
}
