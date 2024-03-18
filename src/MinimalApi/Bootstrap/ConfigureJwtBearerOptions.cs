using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;

namespace MinimalApi;

internal class ConfigureJwtBearerOptions : IConfigureNamedOptions<JwtBearerOptions>
{
    private readonly GoogleConfig _googleConfig;
    private readonly OAuthConfig _oAuthConfig;

    public ConfigureJwtBearerOptions(
        IOptions<GoogleConfig> googleConfigOptions,
        IOptions<OAuthConfig> oAuthConfigOptions)
    {
        _googleConfig = googleConfigOptions.Value;
        _oAuthConfig = oAuthConfigOptions.Value;
    }

    public void Configure(JwtBearerOptions options)
    {
        Configure(JwtBearerDefaults.AuthenticationScheme, options);
    }

    public void Configure(string name, JwtBearerOptions options)
    {
#if DEBUG
        IdentityModelEventSource.ShowPII = true;
        IdentityModelEventSource.LogCompleteSecurityArtifact = true;
#endif

        options.Authority = _oAuthConfig.AuthorityUrl;
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters()
        {
            ValidateIssuerSigningKey = true,
            ValidateAudience = true,
            ValidAudience = _googleConfig.PortalClientId,
            ValidateIssuer = true,
            ValidIssuer = _oAuthConfig.AuthorityUrl,
            ValidateLifetime = true
        };
    }
}
