using System;
using System.Linq;
using System.Threading.Tasks;

using Amazon.DynamoDBv2;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MinimalApi.Services;
using NSubstitute;

namespace MinimalApi.Tests;

public class ProjectAuthorizationTests : IntegrationTestBase
{
    public ProjectAuthorizationTests()
        : base (testSeedDataFileName: "ProjectAuthorizationTestData.json")
    {
    }

    [Theory]
    [InlineData("user-one", 2)]
    [InlineData("user-two", 2)]
    [InlineData("user-three", 1)]
    public async Task GetProjectsReturnsExpected(string userSub, int expectedCount)
    {
        var projectService = ServiceProvider.GetRequiredService<IProjectService>();
        var principal = await ServiceProvider.GetRequiredService<ClaimsPrincipalFactory>()
            .GetClaimsPrincipal(userSub);

        var projectsResult = await projectService.GetProjects(principal);

        Assert.True(projectsResult.AuthorizationResult.Succeeded);

        Assert.Equal(expectedCount, projectsResult.Result.Count());
    }

    [Theory]
    [InlineData("user-one", "project-one", true)] // admin
    [InlineData("user-two", "project-one", true)] // collaborator
    [InlineData("user-three", "project-one", true)] // viewer
    [InlineData("user-one", "project-two", true)] // collaborator
    [InlineData("user-two", "project-two", true)] // collaborator
    [InlineData("user-three", "project-two", false)] // nothing
    public async Task GetProjectReturnsExpected(string userSub, string projectId, bool expectedAuthResult)
    {
        var projectService = ServiceProvider.GetRequiredService<IProjectService>();
        var principal = await ServiceProvider.GetRequiredService<ClaimsPrincipalFactory>()
            .GetClaimsPrincipal(userSub);

        var project = await projectService.GetProject(principal, projectId);

        Assert.Equal(expectedAuthResult, project.AuthorizationResult.Succeeded);

        if (!project.AuthorizationResult.Succeeded)
            return;

        Assert.NotNull(project.Result);
        Assert.Equal(projectId, project.Result.Id);
    }

    [Theory]
    [InlineData("user-one", "project-one", true)] // admin
    [InlineData("user-two", "project-one", true)] // collaborater 
    [InlineData("user-three", "project-one", false)] // viewer
    [InlineData("user-one", "project-two", true)] // collaborator
    [InlineData("user-two", "project-two", false)] // viewer 
    [InlineData("user-three", "project-two", false)] // nothing
    public async Task GetProjectUsersReturnsExpected(string userSub, string projectId, bool expectedAuthResult)
    {
        var projectService = ServiceProvider.GetRequiredService<IProjectService>();
        var principal = await ServiceProvider.GetRequiredService<ClaimsPrincipalFactory>()
            .GetClaimsPrincipal(userSub);

        var project = await projectService.GetProjectUsers(principal, projectId);

        Assert.Equal(expectedAuthResult, project.AuthorizationResult.Succeeded);

        if (!project.AuthorizationResult.Succeeded)
            return;

        Assert.NotNull(project.Result);
    }

    [Theory]
    [InlineData("user-one", "project-one", true)] // admin
    [InlineData("user-two", "project-one", true)] // collaborater 
    [InlineData("user-three", "project-one", false)] // viewer
    [InlineData("user-one", "project-two", true)] // collaborator
    [InlineData("user-two", "project-two", false)] // viewer 
    [InlineData("user-three", "project-two", false)] // nothing
    public async Task CreateProjectDataReturnsExpected(
        string userSub,
        string projectId,
        bool expectedAuthResult)
    {
        var dataService = ServiceProvider.GetRequiredService<IDataService>();
        var principal = await ServiceProvider.GetRequiredService<ClaimsPrincipalFactory>()
            .GetClaimsPrincipal(userSub);

        var project = await dataService.CreateProjectData(
            principal,
            projectId,
            new DataRecordParams()
            {
                DataTypeId = "bmp",
                FileName = "peppers.bmp",
                Size = 263168
            });

        if (expectedAuthResult != project.AuthorizationResult.Succeeded)
            Console.WriteLine("break");

        Assert.Equal(expectedAuthResult, project.AuthorizationResult.Succeeded);

        if (!project.AuthorizationResult.Succeeded)
            return;

        Assert.NotNull(project.Result);
    }

    [Theory]
    [InlineData("user-one", "project-one", true)] // admin
    [InlineData("user-two", "project-one", true)] // collaborater 
    [InlineData("user-three", "project-one", false)] // viewer
    [InlineData("user-one", "project-two", true)] // collaborator
    [InlineData("user-two", "project-two", false)] // viewer 
    [InlineData("user-three", "project-two", false)] // nothing
    public async Task GetProjectDataReturnsExpected(
        string userSub,
        string projectId,
        bool expectedAuthResult)
    {
        var dataService = ServiceProvider.GetRequiredService<IDataService>();
        var principal = await ServiceProvider.GetRequiredService<ClaimsPrincipalFactory>()
            .GetClaimsPrincipal(userSub);

        var project = await dataService.GetProjectData(principal, projectId);

        Assert.Equal(expectedAuthResult, project.AuthorizationResult.Succeeded);

        if (!project.AuthorizationResult.Succeeded)
            return;

        Assert.NotNull(project.Result);
    }

    [Fact]
    public async Task ProjectMembershipRoleIsAssignedUponProjectUserCreation()
    {
        var projectId = "project-one";

        var principal = await ServiceProvider.GetRequiredService<ClaimsPrincipalFactory>()
            .GetClaimsPrincipal("user-one");

        var mockClaimsService = Substitute.For<IAuthClaimsService>();
        mockClaimsService
            .CreateUserRole(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>())
            .Returns(
                new UserRole()
                {
                });

        var projectService = new ProjectService(
            ServiceProvider.GetRequiredService<IAmazonDynamoDB>(),
            mockClaimsService,
            ServiceProvider.GetRequiredService<IOptions<DynamoConfig>>());

        var result = await projectService.CreateProjectUser(
            principal,
            projectId,
            "user-three",
            "MinimalApi::Role::ProjectCollaborator");

        Assert.True(result.IsSuccess);

        await mockClaimsService.Received().CreateUserRole(
            Arg.Any<string>(),
            "MinimalApi::Role::ProjectCollaborator",
            $"Project::{projectId}");
    }
}
