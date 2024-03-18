using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace MinimalApi;

public static class HealthEndpoints
{
    public static async Task<IResult> GetHealth(
        [FromServices] IHttpContextAccessor contextAccessor,
        [FromServices] IOptions<AwsConfig> awsConfigOptions,
        [FromServices] IOptions<OAuthConfig> oauthConfigOptions,
        [FromQuery] string? echo)
    {
        return Results.Ok(new HealthResponse()
        {
            Now = DateTime.UtcNow,
            RequestorIp = contextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? string.Empty,
            Echo = echo,

            AwsAccountId = awsConfigOptions.Value.AccountId,
            AllowedOrigins = oauthConfigOptions.Value.AllowedOrigins
        });
    }

    public static async Task<IResult> PostHealth(
        [FromServices] IHttpContextAccessor contextAccessor,
        [FromBody] HealthRequest request)
    {
        return Results.Ok(new HealthResponse()
        {
            Now = DateTime.UtcNow,
            Then = request.Now,
            RequestorIp = contextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? string.Empty,
            Echo = request.Echo
        });
    }
}
