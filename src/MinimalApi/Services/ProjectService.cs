using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace MinimalApi.Services;

public interface IProjectService
{
    Task<ServiceResult<Project>> CreateProject(
        ClaimsPrincipal principal,
        string projectName,
        string description,
        string metadata);
    Task<ServiceResult<Project>> GetProject(ClaimsPrincipal principal, string projectId);
    Task<ServiceResult<IEnumerable<Project>>> GetProjects(ClaimsPrincipal principal);
    Task<ServiceResult> CreateProjectUser(ClaimsPrincipal principal, string projectId, string userId, string role);
    Task<ServiceResult<IEnumerable<ProjectUser>>> GetProjectUsers(ClaimsPrincipal principal, string projectId);
    Task<Project> SaveProject(Project project);
}

public class ProjectService : IProjectService
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IAmazonDynamoDB _dynamoClient;
    private readonly IUserRoleService _userRoleService;
    private readonly DynamoConfig _dynamoConfig;

    public ProjectService(
        IAuthorizationService authorizationService,
        IAmazonDynamoDB dynamoClient,
        IUserRoleService userRoleService,
        IOptions<DynamoConfig> dynamoConfigOptions)
    {
        _authorizationService = authorizationService;
        _dynamoClient = dynamoClient;
        _userRoleService = userRoleService;
        _dynamoConfig = dynamoConfigOptions.Value;
    }

    public async Task<ServiceResult<Project>> CreateProject(
        ClaimsPrincipal principal,
        string name,
        string description,
        string metadata)
    {
        var userId = principal.GetPrincipalIdentity();

        var existing = await GetProject(userId, name);

        if (existing != default)
            return ServiceResult<Project>.Failure();

        // TODO: transaction
        {
            var projectId = Guid.NewGuid().ToString();

            var project = new Project()
            {
                Id = projectId,
                Name = name,
                Description = description,
                DataPath = $"s3://minimal-api.erroll.io/data/projects/{projectId}/",
                Metadata = metadata,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            await SaveProject(project);

            await _userRoleService.CreateUserRole(
                userId,
                "MinimalApi::Role::ProjectOwner",
                $"MinimalApi::Project:{projectId}");

            return ServiceResult<Project>.Success(project);
        }
    }

    public async Task<ServiceResult<Project>> GetProject(ClaimsPrincipal principal, string projectId)
    {
        var authResult = await _authorizationService.AuthorizeAsync(
            principal,
            new OperationRequirement()
            {
                Operation = "MinimalApi::Action::ReadProject",
                Condition = $"MinimalApi::Project:{projectId}"
            });

        if (!authResult.Succeeded)
        {
            return ServiceResult<Project>.Forbidden(authResult);
        }

        var project = await GetProject(projectId);

        return ServiceResult<Project>.Success(project, authResult);
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

    public async Task<Project> GetProject(string userId, string name)
    {
        var response = await _dynamoClient.QueryAsync(
            new QueryRequest()
            {
                TableName = _dynamoConfig.ProjectsTableName,
                IndexName = _dynamoConfig.ProjectsTableCreatedByIndexName,
                KeyConditions = new Dictionary<string, Condition>()
                {
                    ["created_by"] = new Condition()
                    {
                        ComparisonOperator = ComparisonOperator.EQ,
                        AttributeValueList = new List<AttributeValue>()
                        {
                            new AttributeValue(userId)
                        }
                    },
                    ["name"] = new Condition()
                    {
                        ComparisonOperator = ComparisonOperator.EQ,
                        AttributeValueList = new List<AttributeValue>()
                        {
                            new AttributeValue(name)
                        }
                    }
                }
            });

        if (!response.Items.Any())
            return default;

        return ToProject(response.Items.First());
    }

    // TODO: pagination
    // TODO: projection expression?
    public async Task<ServiceResult<IEnumerable<Project>>> GetProjects(ClaimsPrincipal principal)
    {
        var projectIds = await _userRoleService.GetUserRoleConditionValues(
            principal,
            "MinimalApi::Action::ReadProject",
            "MinimalApi::Project");

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

    public async Task<ServiceResult> CreateProjectUser(
        ClaimsPrincipal principal,
        string projectId,
        string userId,
        string role)
    {
        var authResult = await _authorizationService.AuthorizeAsync(
            principal,
            new OperationRequirement()
            {
                Operation = "MinimalApi::Action::CreateProjectUser",
                Condition = $"MinimalApi::Project:{projectId}"
            });

        if (!authResult.Succeeded)
        {
            return ServiceResult<Project>.Forbidden(authResult);
        }

        // TODO: better role validation
        if (!role.StartsWith("MinimalApi::Role::Project"))
        {
            return ServiceResult.Failure("Invalid role.");
        }

        await _userRoleService.CreateUserRole(
            userId,
            role,
            $"MinimalApi::Project:{projectId}");

        return ServiceResult.Success();
    }

    public async Task<ServiceResult<IEnumerable<ProjectUser>>> GetProjectUsers(
        ClaimsPrincipal principal,
        string projectId)
    {
        var authorizationResult = await _authorizationService.AuthorizeAsync(
            principal,
            new OperationRequirement(
                "MinimalApi::Action::ReadProjectData",
                $"MinimalApi::Project:{projectId}"));

        if (!authorizationResult.Succeeded)
            return ServiceResult<IEnumerable<ProjectUser>>.Forbidden();

        var userRoles = await _userRoleService.GetUserRolesByRoleCondition(
            "MinimalApi::Role::Project",
            $"MinimalApi::Project:{projectId}",
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

        if (project.CreatedBy == default)
            throw new Exception("Missing CreatedBy.");

        item["created_by"] = new AttributeValue(project.CreatedBy);

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
            CreatedBy = item.ContainsKey("created_by") ? item["created_by"].S : default,
            CreatedAt = item.ContainsKey("created_at") ? DateTime.Parse(item["created_at"].S) : default,
            ModifiedAt = item.ContainsKey("modified_at") ? DateTime.Parse(item["modified_at"].S) : default,
        };
    }
}
