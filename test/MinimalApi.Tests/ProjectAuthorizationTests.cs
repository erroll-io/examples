using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Amazon.DynamoDBv2;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MinimalApi.Tests;

public abstract class IntegrationTests
{
    protected IServiceProvider ServiceProvider { get; private set; }
    protected DynamoConfig DynamoConfig =>
        new DynamoConfig()
        {
            RolesTableName = "roles",
            PermissionsTableName = "permissions",
            RolePermissionsTableName = "role_permissions",
            UserRolesTableName = "user_roles",
            UsersTableName = "users",
            ProjectsTableName = "projects",
            DataTableName = "data",
            ProjectDataTableName = "project_data",
        };

    protected IntegrationTests(string testSeedDataFileName = default)
    {
        var builder = WebApplication.CreateSlimBuilder()
            .ConfigureApplicationServices();

        builder.Services.AddSingleton(Options.Create(DynamoConfig));
        builder.Services.AddTransient<IAmazonDynamoDB>(provider =>
            new InMemoryDynamoClient(
                new Dictionary<string, string>()
                {
                    ["projects"] = "id",
                    ["roles"] = "id",
                    ["permissions"] = "id",
                    ["role_permissions"] = "role_id",
                    ["users"] = "id",
                    ["user_roles"] = "id"
                },
                new Dictionary<string, string>()
                {
                    ["role_permissions"] = "permission_id"
                }));
        builder.Services.AddSingleton<TestSeeder>();
        
        ServiceProvider = builder.Build().Services;

        var seeder = ServiceProvider.GetRequiredService<TestSeeder>();
        seeder.SeedData(testSeedDataFileName).Wait();
    }
}

public class ProjectAuthorizationTests : IntegrationTests
{
    public ProjectAuthorizationTests()
        : base (testSeedDataFileName: "ProjectAuthorizationTestData.json")
    {
    }

    [Fact]
    public async Task CanFoo()
    {
        var dynamoClient = ServiceProvider.GetRequiredService<IAmazonDynamoDB>();
    }
}
