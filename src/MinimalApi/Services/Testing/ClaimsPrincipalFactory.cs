using System.Security.Claims;
using System.Threading.Tasks;

namespace MinimalApi.Services;

public class ClaimsPrincipalFactory
{
    private readonly AuthClaimsTransformation _claimsTransformation;

    public ClaimsPrincipalFactory(AuthClaimsTransformation claimsTransformation = null)
    {
        _claimsTransformation = claimsTransformation;
    }

    public async Task<ClaimsPrincipal> GetClaimsPrincipal(string principalId)
    {
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(
                new []
                {
                    string.IsNullOrEmpty(principalId)
                        ? new Claim("wack", "AF")
                        : new Claim("sub", principalId)
                },
                string.IsNullOrEmpty(principalId) ? string.Empty : "Bearer"));

        if (_claimsTransformation != default)
        {
            principal = await _claimsTransformation.TransformAsync(principal);
        }

        return principal;
    }
}
