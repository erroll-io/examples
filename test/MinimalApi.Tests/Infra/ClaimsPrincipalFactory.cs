using System.Security.Claims;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication;

namespace MinimalApi.Tests;

public class ClaimsPrincipalFactory
{
    private readonly AuthClaimsTransformation _claimsTransformation;

    public ClaimsPrincipalFactory(AuthClaimsTransformation claimsTransformation)
    {
        _claimsTransformation = claimsTransformation;
    }

    public Task<ClaimsPrincipal> GetClaimsPrincipal(string principalId)
    {
        return _claimsTransformation.TransformAsync(
            new ClaimsPrincipal(
                new ClaimsIdentity(
                    new []
                    {
                        string.IsNullOrEmpty(principalId)
                            ? new Claim("wack", "AF")
                            : new Claim("sub", principalId)
                    },
                    string.IsNullOrEmpty(principalId) ? string.Empty : "Bearer")));
    }
}
