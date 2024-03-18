using Microsoft.AspNetCore.Builder;

namespace MinimalApi;

public static class WebApplicationExtensions
{
    internal const string CorsPolicyName = "default";

    public static WebApplication ConfigureApplication(this WebApplication app)
    {
        return app.UseAuthentication()
            //.UseAuthorization()
            .UseCors() as WebApplication;
    }

    public static WebApplication RegisterEndpoints(this WebApplication app)
    {
        app.MapGet("/health", HealthEndpoints.GetHealth);
        app.MapPost("/health", HealthEndpoints.PostHealth);
        app.MapPost("/users", UsersEndpoints.CreateUser);
        app.MapGet("/users/{id}", UsersEndpoints.GetUser);
        app.MapGet("/users/current", UsersEndpoints.GetCurrentUser);
        app.MapPut("/users/{id}", UsersEndpoints.UpdateUser);
        app.MapPut("/users/current", UsersEndpoints.UpdateCurrentUser);

        return app;
    }
}
