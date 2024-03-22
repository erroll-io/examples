using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

using Microsoft.Extensions.Options;

namespace MinimalApi.Services;

public interface IProjectService
{
    Task<Project> GetProject(string projectId);
    Task<IEnumerable<Project>> GetProjects(IEnumerable<string> projectIds);
    Task<Project> SaveProject(Project project);
    Task<ProjectUser> GetProjectUser(string projectUserId);
    Task<ProjectUser> GetProjectUser(string projectId, string userId);
    Task<ProjectUser> SaveProjectUser(ProjectUser projectUser);
}

public class ProjectService : IProjectService
{
    private readonly IAmazonDynamoDB _dynamoClient;
    private readonly DynamoConfig _dynamoConfig;

    public ProjectService(IAmazonDynamoDB dynamoClient, IOptions<DynamoConfig> dynamoConfigOptions)
    {
        _dynamoClient = dynamoClient;
        _dynamoConfig = dynamoConfigOptions.Value;
    }

    public async Task<Project> GetProject(string projectId)
    {
        var project = await _dynamoClient.GetItemAsync(
            _dynamoConfig.ProjectsTableName,
            new Dictionary<string, AttributeValue>()
            {
                ["id"] = new AttributeValue(projectId)
            });

        return ToProject(project.Item);
    }

    // TODO: pagination
    public async Task<IEnumerable<Project>> GetProjects(IEnumerable<string> projectIds)
    {
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

        return projects.Responses
            .SelectMany(response => response.Value)
            .Select(response => ToProject(response));
    }

    public async Task<Project> SaveProject(Project project)
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

        await _dynamoClient.PutItemAsync(new PutItemRequest()
        {
            TableName = _dynamoConfig.ProjectsTableName,
            Item = item
        });

        return project;
    }

    public async Task<ProjectUser> GetProjectUser(string projectUserId)
    {
        var projectUserResponse = await _dynamoClient.GetItemAsync(
            _dynamoConfig.ProjectUsersTableName,
            new Dictionary<string, AttributeValue>()
            {
                ["id"] = new AttributeValue(projectUserId)
            });

        return ToProjectUser(projectUserResponse.Item);
    }

    public async Task<ProjectUser> GetProjectUser(string projectId, string userId)
    {
        var response = await _dynamoClient.QueryAsync(
            new QueryRequest()
            {
                TableName = _dynamoConfig.ProjectUsersTableName,
                KeyConditions = new Dictionary<string, Condition>()
                {
                    ["project_id"] = new Condition()
                    {
                        ComparisonOperator = ComparisonOperator.EQ,
                        AttributeValueList = new List<AttributeValue>()
                        {
                            new AttributeValue(projectId)
                        }
                    },
                    ["user_id"] = new Condition()
                    {
                        ComparisonOperator = ComparisonOperator.EQ,
                        AttributeValueList = new List<AttributeValue>()
                        {
                            new AttributeValue(userId)
                        }
                    }
                }
            });

        if (response.Items == default || !response.Items.Any())
        {
            return default;
        }

        return ToProjectUser(response.Items.SingleOrDefault());
    }

    public async Task<ProjectUser> SaveProjectUser(ProjectUser projectUser)
    {
        var item = new Dictionary<string, AttributeValue>();

        if (string.IsNullOrEmpty(projectUser.Id))
            throw new Exception("Missing ID.");

        item["id"] = new AttributeValue(projectUser.Id);

        if (string.IsNullOrEmpty(projectUser.ProjectId))
            throw new Exception("Missing Project ID.");

        item["project_id"] = new AttributeValue(projectUser.ProjectId);

        if (string.IsNullOrEmpty(projectUser.UserId))
            throw new Exception("Missing Project ID.");

        item["user_id"] = new AttributeValue(projectUser.UserId);

        if (projectUser.CreatedAt == default)
            throw new Exception("Missing CreatedAt.");

        item["created_at"] = new AttributeValue(projectUser.CreatedAt.ToUniversalTime().ToString("o"));

        item["modified_at"] = new AttributeValue(DateTime.UtcNow.ToUniversalTime().ToString("o"));

        await _dynamoClient.PutItemAsync(new PutItemRequest()
        {
            TableName = _dynamoConfig.ProjectUsersTableName,
            Item = item
        });

        return projectUser;
    }

    private Project ToProject(Dictionary<string, AttributeValue> item)
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

    private ProjectUser ToProjectUser(Dictionary<string, AttributeValue> item)
    {
        if (item == default || !item.Any())
            return default;

        return new ProjectUser()
        {
            Id = item.ContainsKey("id") ? item["id"].S : default,
            ProjectId = item.ContainsKey("project_id") ? item["project_id"].S : default,
            UserId = item.ContainsKey("user_id") ? item["user_id"].S : default,
            CreatedAt = item.ContainsKey("created_at") ? DateTime.Parse(item["created_at"].S) : default,
            ModifiedAt = item.ContainsKey("modified_at") ? DateTime.Parse(item["modified_at"].S) : default,
        };
    }
}
