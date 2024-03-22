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
        var projectIds = httpContextAccessor.HttpContext.User.GetResourceIdsForPermission(
            "MinimalApi::Action::ReadProject",
            "Project");

        var projects = await projectService.GetProjects(projectIds);

        return Results.Ok(new ProjectsResponse()
        {
            Projects = projects.Select(ToResponse)
        });
    }

    [Authorize]
    public static async Task<IResult> GetProject(
        [FromServices] IHttpContextAccessor httpContextAccessor,
        [FromServices] IProjectService projectService,
        string id)
    {
        if (!httpContextAccessor.HttpContext.User.HasPermission(
            "MinimalApi::Action::ReadProject",
            $"Project::{id}"))
        {
            return Results.Forbid();
        }

        if (string.IsNullOrEmpty(id))
            return Results.BadRequest(nameof(id));

        var project = await projectService.GetProject(id);

        return Results.Ok(project.ToResponse());
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
}
