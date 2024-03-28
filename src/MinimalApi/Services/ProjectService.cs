using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

using Microsoft.Extensions.Options;

namespace MinimalApi.Services;

public interface IProjectService
{
    Task<ServiceResult<Project>> GetProject(ClaimsPrincipal principal, string projectId);
    Task<ServiceResult<IEnumerable<Project>>> GetProjects(ClaimsPrincipal principal);
    Task<ServiceResult<IEnumerable<ProjectUser>>> GetProjectUsers(ClaimsPrincipal principal, string projectId);
    Task<Project> SaveProject(Project project);
}

public class ProjectService : IProjectService
{
    private readonly IAmazonDynamoDB _dynamoClient;
    private readonly IAuthClaimsService _authClaimsService;
    private readonly DynamoConfig _dynamoConfig;

    public ProjectService(
        IAmazonDynamoDB dynamoClient,
        IAuthClaimsService authClaimsService,
        IOptions<DynamoConfig> dynamoConfigOptions)
    {
        _dynamoClient = dynamoClient;
        _authClaimsService = authClaimsService;
        _dynamoConfig = dynamoConfigOptions.Value;
    }

    public async Task<ServiceResult<Project>> GetProject(ClaimsPrincipal principal, string projectId)
    {
        if (!principal.HasPermission(
            "MinimalApi::Action::ReadProject",
            $"Project::{projectId}"))
        {
            return ServiceResult<Project>.Forbidden();
        }

        var project = await GetProject(projectId);

        return ServiceResult<Project>.Success(project);
    }

    public async Task<Project> GetProject(string projectId)
    {
        var projectResponse = await _dynamoClient.GetItemAsync(
            _dynamoConfig.ProjectsTableName,
            new Dictionary<string, AttributeValue>()
            {
                ["id"] = new AttributeValue(projectId)
            });

        return ToProject(projectResponse.Item);
    }

    // TODO: pagination
    public async Task<ServiceResult<IEnumerable<Project>>> GetProjects(ClaimsPrincipal principal)
    {
        var projectIds = principal.GetResourceIdsForPermissionCondition(
            "MinimalApi::Action::ReadProject",
            "Project");

        if (!projectIds.Any())
        {
            return ServiceResult<IEnumerable<Project>>.Success(Enumerable.Empty<Project>());
        }

        var projects = await _dynamoClient.BatchGetItemAsync(
            new BatchGetItemRequest()
            {
                RequestItems  = new Dictionary<string, KeysAndAttributes>()
                {
                    [_dynamoConfig.ProjectsTableName] = new KeysAndAttributes()
                    {
                        Keys = projectIds.Select(projectId =>
                            new Dictionary<string, AttributeValue>()
                            {
                                ["id"] = new AttributeValue(projectId)
                            }).ToList()
                    }
                }
            });

        return ServiceResult<IEnumerable<Project>>.Success(
            projects.Responses
                .SelectMany(response => response.Value)
                .Select(response => ToProject(response)));
    }

    public async Task<ServiceResult<IEnumerable<ProjectUser>>> GetProjectUsers(
        ClaimsPrincipal principal,
        string projectId)
    {
        if (!principal.HasPermission(
            "MinimalApi::Action::ReadProjectData",
            $"Project::{projectId}"))
        {
            return ServiceResult<IEnumerable<ProjectUser>>.Forbidden();
        }

        var userRoles = await _authClaimsService.GetUserRolesByCondition(
            "MinimalApi::Role::Project",
            $"Project::{projectId}",
            "BEGINS_WITH");

        if (userRoles == default || !userRoles.Any())
        {
            return ServiceResult<IEnumerable<ProjectUser>>.Success(Enumerable.Empty<ProjectUser>());
        }

        return ServiceResult<IEnumerable<ProjectUser>>.Success(userRoles.Select(userRole =>
            new ProjectUser()
            {
                UserId = userRole.UserId,
                UserName = "TODO",
                Role = userRole.RoleId
            }));
    }

    public async Task<Project> SaveProject(Project project)
    {
        var item = FromProject(project);

        await _dynamoClient.PutItemAsync(new PutItemRequest()
        {
            TableName = _dynamoConfig.ProjectsTableName,
            Item = item
        });

        return project;
    }

    internal static Dictionary<string, AttributeValue> FromProject(Project project)
    {
        var item = new Dictionary<string, AttributeValue>();

        if (string.IsNullOrEmpty(project.Id))
            throw new Exception("Missing project ID.");

        item["id"] = new AttributeValue(project.Id);

        if (!string.IsNullOrEmpty(project.Name))
            item["name"] = new AttributeValue(project.Name);

        if (!string.IsNullOrEmpty(project.Description))
            item["description"] = new AttributeValue(project.Description);

        if (!string.IsNullOrEmpty(project.DataPath))
            item["data_path"] = new AttributeValue(project.DataPath);

        if (!string.IsNullOrEmpty(project.Metadata))
            item["metadata"] = new AttributeValue(project.Metadata);

        if (project.CreatedAt == default)
            throw new Exception("Missing CreatedAt.");

        item["created_at"] = new AttributeValue(project.CreatedAt.ToUniversalTime().ToString("o"));

        item["modified_at"] = new AttributeValue(DateTime.UtcNow.ToUniversalTime().ToString("o"));

        return item;
    }

    internal static Project ToProject(Dictionary<string, AttributeValue> item)
    {
        if (item == default || !item.Any())
            return default;

        return new Project()
        {
            Id = item.ContainsKey("id") ? item["id"].S : default,
            Name = item.ContainsKey("name") ? item["name"].S : default,
            Description = item.ContainsKey("description") ? item["description"].S : default,
            DataPath = item.ContainsKey("data_path") ? item["data_path"].S : default,
            Metadata = item.ContainsKey("metadata") ? item["metadata"].S : default,
            CreatedAt = item.ContainsKey("created_at") ? DateTime.Parse(item["created_at"].S) : default,
            ModifiedAt = item.ContainsKey("modified_at") ? DateTime.Parse(item["modified_at"].S) : default,
        };
    }
}
