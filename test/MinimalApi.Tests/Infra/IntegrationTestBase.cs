using System;
using System.Collections.Generic;

using Amazon.DynamoDBv2;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MinimalApi.Tests;

public abstract class IntegrationTestBase
{
    protected static DynamoConfig DynamoConfig =>
        new DynamoConfig()
        {
            RolesTableName = "roles",
            PermissionsTableName = "permissions",
            RolePermissionsTableName = "role_permissions",
            UserRolesTableName = "user_roles",
            UsersTableName = "users",
            ProjectsTableName = "projects",
            DataTableName = "data_records",
            ProjectDataTableName = "project_data",
        };

    protected IServiceProvider ServiceProvider { get; private set; }

    protected IntegrationTestBase(
        string testSeedDataFileName = default,
        Action<IServiceCollection> configureServices = default)
    {
        var builder = WebApplication.CreateSlimBuilder()
            .ConfigureApplicationServices();

        builder.Services.AddSingleton<AuthClaimsTransformation>();
        builder.Services.AddSingleton<ClaimsPrincipalFactory>();

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
                    ["user_roles"] = "id",
                    ["data_records"] = "id",
                    ["project_data"] = "project_id",
                },
                new Dictionary<string, string>()
                {
                    ["role_permissions"] = "permission_id",
                    ["project_data"] = "data_record_id",
                }));
        builder.Services.AddSingleton<TestSeeder>();

        configureServices?.Invoke(builder.Services);
        
        ServiceProvider = builder.Build().Services;

        var seeder = ServiceProvider.GetRequiredService<TestSeeder>();
        seeder.SeedData(testSeedDataFileName).Wait();
    }
}
