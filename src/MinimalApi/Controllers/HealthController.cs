using Microsoft.AspNetCore.Mvc;

namespace MinimalApi;

public static class HealthController
{
    public static async Task<IResult> GetHealth(
        [FromServices] IHttpContextAccessor contextAccessor,
        [FromQuery] string? echo)
    {
        return Results.Ok(new HealthResponse()
        {
            Now = DateTime.UtcNow,
            RequestorIp = contextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? string.Empty,
            Echo = echo
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
