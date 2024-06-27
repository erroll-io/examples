using Amazon;
using Microsoft.AspNetCore.Builder;

namespace MinimalApi;

public static class WebApplicationExtensions
{
    internal const string CorsPolicyName = "default";

    public static WebApplication ConfigureApplication(this WebApplication app)
    {
        return app
            .UseAuthentication()
            .UseAuthorization()
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

        app.MapPost("/projects", ProjectsEndpoints.CreateProject);
        app.MapGet("/projects", ProjectsEndpoints.GetProjects);
        app.MapGet("/projects/{projectId}", ProjectsEndpoints.GetProject);
        app.MapGet("/projects/{projectId}/users", ProjectsEndpoints.GetProjectUsers);

        app.MapPost("/data", DataEndpoints.CreateUserData);
        app.MapGet("/data", DataEndpoints.GetUserData);
        app.MapPost("/projects/{projectId}/data", DataEndpoints.CreateProjectData);
        app.MapGet("/projects/{projectId}/data", DataEndpoints.GetProjectData);

        app.MapGet("tests/authz-comparison", TestEndpoints.CompareAuthorizationStrategies);
        app.MapGet("tests/cedar-timing", TestEndpoints.TestCedarPolicySize);

        return app;
    }
}
