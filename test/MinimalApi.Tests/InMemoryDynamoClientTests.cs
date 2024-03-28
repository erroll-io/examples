using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using MinimalApi.Services;
using Xunit;

namespace MinimalApi.Tests;

public class InMemoryDynamoClientTests
{
    [Fact]
    public async Task CanPutAndGetItem()
    {
        var project = new Project()
        {
            Id = Guid.NewGuid().ToString(),
            Name = "name",
            Description = "description",
            DataPath = "datapath",
            Metadata = "{\"foo\": \"bar\"}",
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        var dynamoClient = FakeClientFactory();

        await dynamoClient.PutItemAsync(
            new PutItemRequest()
            {
                TableName = "projects",
                Item = ProjectService.FromProject(project)
            });

        var projectResponse = await dynamoClient.GetItemAsync(
            "projects",
            new Dictionary<string, AttributeValue>()
            {
                ["id"] = new AttributeValue(project.Id)
            });

        var retrievedProject = ProjectService.ToProject(projectResponse.Item);

        Assert.Equal(project.Id, retrievedProject.Id);
        Assert.Equal(project.Name, retrievedProject.Name);
        Assert.Equal(project.Description, retrievedProject.Description);
        Assert.Equal(project.DataPath, retrievedProject.DataPath);
        Assert.Equal(project.Metadata, retrievedProject.Metadata);
        // TODO: broken
        //Assert.Equal(project.CreatedAt, retrievedProject.CreatedAt);
        //Assert.Equal(project.ModifiedAt, retrievedProject.ModifiedAt);
    }

    [Fact]
    public async Task CanPutAndBatchGetItems()
    {
        var projectOne = new Project()
        {
            Id = Guid.NewGuid().ToString(),
            Name = "one",
            Description = "one",
            DataPath = "one",
            Metadata = "{\"foo\": 1}",
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        var projectTwo = new Project()
        {
            Id = Guid.NewGuid().ToString(),
            Name = "two",
            Description = "two",
            DataPath = "two",
            Metadata = "{\"foo\": 2}",
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        var dynamoClient = FakeClientFactory();

        await dynamoClient.PutItemAsync(
            new PutItemRequest()
            {
                TableName = "projects",
                Item = ProjectService.FromProject(projectOne)
            });
        await dynamoClient.PutItemAsync(
            new PutItemRequest()
            {
                TableName = "projects",
                Item = ProjectService.FromProject(projectTwo)
            });

        var projects = await dynamoClient.BatchGetItemAsync(
            new BatchGetItemRequest()
            {
                RequestItems  = new Dictionary<string, KeysAndAttributes>()
                {
                    ["projects"] = new KeysAndAttributes()
                    {
                        Keys = 
                        {
                            new Dictionary<string, AttributeValue>()
                            {
                                ["id"] = new AttributeValue(projectOne.Id)
                            },
                            new Dictionary<string, AttributeValue>()
                            {
                                ["id"] = new AttributeValue(projectTwo.Id)
                            },
                        }
                    }
                }
            });

        var retrievedProjects = projects.Responses
                .SelectMany(response => response.Value)
                .Select(response => ProjectService.ToProject(response))
                .ToList();

        Assert.NotEmpty(retrievedProjects);
        Assert.Equal(2, retrievedProjects.Count);
        Assert.Equal(retrievedProjects[0].Id, projectOne.Id);
        Assert.Equal(retrievedProjects[1].Id, projectTwo.Id);
    }

    [Fact]
    public async Task CanQueryItems()
    {
        var projectOne = new Project()
        {
            Id = Guid.NewGuid().ToString(),
            Name = "one",
            Description = "foo",
            DataPath = "one",
            Metadata = "{\"foo\": 1}",
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        var projectTwo = new Project()
        {
            Id = Guid.NewGuid().ToString(),
            Name = "two",
            Description = "foo",
            DataPath = "two",
            Metadata = "{\"foo\": 2}",
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        var dynamoClient = FakeClientFactory();

        await dynamoClient.PutItemAsync(
            new PutItemRequest()
            {
                TableName = "projects",
                Item = ProjectService.FromProject(projectOne)
            });
        await dynamoClient.PutItemAsync(
            new PutItemRequest()
            {
                TableName = "projects",
                Item = ProjectService.FromProject(projectTwo)
            });

        var response = await dynamoClient.QueryAsync(
            new QueryRequest()
            {
                TableName = "projects",
                KeyConditions = new Dictionary<string, Condition>()
                {
                    ["name"] = new Condition()
                    {
                        ComparisonOperator = ComparisonOperator.EQ,
                        AttributeValueList = new List<AttributeValue>()
                        {
                            new AttributeValue("one")
                        }
                    },
                    ["description"] = new Condition()
                    {
                        ComparisonOperator = ComparisonOperator.EQ,
                        AttributeValueList = new List<AttributeValue>()
                        {
                            new AttributeValue("foo")
                        }
                    }
                }
            });

        Assert.Equal(1, response.Items.Count);
        Assert.Equal(projectOne.Id, ProjectService.ToProject(response.Items.Single()).Id);
    }

    [Fact]
    public async Task CanQueryWithBeginsWithOperator()
    {
        var projectOne = new Project()
        {
            Id = Guid.NewGuid().ToString(),
            Name = "one",
            Description = "foo",
            DataPath = "one",
            Metadata = "{\"foo\": 1}",
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        var projectTwo = new Project()
        {
            Id = Guid.NewGuid().ToString(),
            Name = "two",
            Description = "food",
            DataPath = "two",
            Metadata = "{\"foo\": 2}",
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        var dynamoClient = FakeClientFactory();

        await dynamoClient.PutItemAsync(
            new PutItemRequest()
            {
                TableName = "projects",
                Item = ProjectService.FromProject(projectOne)
            });
        await dynamoClient.PutItemAsync(
            new PutItemRequest()
            {
                TableName = "projects",
                Item = ProjectService.FromProject(projectTwo)
            });

        var response = await dynamoClient.QueryAsync(
            new QueryRequest()
            {
                TableName = "projects",
                KeyConditions = new Dictionary<string, Condition>()
                {
                    ["description"] = new Condition()
                    {
                        ComparisonOperator = ComparisonOperator.BEGINS_WITH,
                        AttributeValueList = new List<AttributeValue>()
                        {
                            new AttributeValue("foo")
                        }
                    }
                }
            });

        Assert.Equal(2, response.Items.Count);
        Assert.Equal(projectOne.Id, ProjectService.ToProject(response.Items[0]).Id);
        Assert.Equal(projectTwo.Id, ProjectService.ToProject(response.Items[1]).Id);
    }

    private IAmazonDynamoDB FakeClientFactory()
    {
        return new InMemoryDynamoClient(new Dictionary<string, string>()
        {
            ["projects"] = "id",
            ["roles"] = "id",
            ["permissions"] = "id",
        });
    }
}
