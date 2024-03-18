using System.Linq;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;

namespace MinimalApi;

internal class ConfigureCorsOptions : IConfigureNamedOptions<CorsOptions>
{
    private readonly OAuthConfig _oAuthConfig;

    public ConfigureCorsOptions(IOptions<OAuthConfig> oAuthConfig)
    {
        _oAuthConfig = oAuthConfig.Value;
    }

    public void Configure(CorsOptions opts)
    {
        Configure(WebApplicationExtensions.CorsPolicyName, opts);
    }

    public void Configure(string? name, CorsOptions opts)
    {
        if (string.IsNullOrEmpty(name))
        {
            name = WebApplicationExtensions.CorsPolicyName;
        }

        opts.AddPolicy(
            name,
            builder =>
            {
                builder.AllowAnyMethod();
                builder.AllowAnyHeader();
                builder.AllowCredentials();
                builder.WithOrigins(_oAuthConfig.AllowedOrigins.ToArray());
            });
    }
}
