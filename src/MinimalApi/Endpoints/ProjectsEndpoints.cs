using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using MinimalApi.Services;

namespace MinimalApi;

public static class ProjectsEndpoints
{
    [Authorize]
    public static async Task<IResult> GetProjects(
        [FromServices] IHttpContextAccessor httpContextAccessor,
        [FromServices] IProjectService projectService)
    {
        var projectsResult = await projectService.GetProjects(httpContextAccessor.HttpContext.User);

        return projectsResult.FromServiceResult(projects =>
            new ProjectsResponse()
            {
                Projects = projects.Select(ToResponse)
            });
    }

    [Authorize]
    public static async Task<IResult> GetProject(
        [FromServices] IHttpContextAccessor httpContextAccessor,
        [FromServices] IProjectService projectService,
        string projectId)
    {
        if (string.IsNullOrEmpty(projectId))
            return Results.BadRequest(nameof(projectId));

        var projectResult = await projectService.GetProject(httpContextAccessor.HttpContext.User, projectId);

        return projectResult.FromServiceResult(ToResponse);
    }

    [Authorize]
    public static async Task<IResult> GetProjectUsers(
        [FromServices] IHttpContextAccessor httpContextAccessor,
        [FromServices] IProjectService projectService,
        string projectId)
    {
        if (string.IsNullOrEmpty(projectId))
            return Results.BadRequest(nameof(projectId));

        var projectUsersResult = await projectService.GetProjectUsers(
            httpContextAccessor.HttpContext.User,
            projectId);

        return projectUsersResult.FromServiceResult(projectUsers =>
            projectUsers.Select(ToResponse));
    }

    public static ProjectResponse ToResponse(this Project project)
    {
        return new ProjectResponse()
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            DataPath = project.DataPath,
            CreatedAt = project.CreatedAt,
            ModifiedAt = project.ModifiedAt
        };
    }

    public static ProjectUserResponse ToResponse(this ProjectUser projectUser)
    {
        return new ProjectUserResponse()
        {
            UserId = projectUser.UserId,
            UserName = projectUser.UserName,
            Role = projectUser.Role
        };
    }
}
