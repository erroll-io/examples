using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

using Microsoft.Extensions.Options;
using System.Data;

namespace MinimalApi.Services;

public interface IRoleService
{
    Task<Role> GetRole(string roleId);
    Task<Role> SaveRole(Role role);
    Task<RolePermission> GetRolePermission(string roleId, string permissionId);
    Task<IEnumerable<RolePermission>> GetRolePermissions(string roleId);
    Task<RolePermission> SaveRolePermission(RolePermission permission);
}

public class RoleService : IRoleService
{
    private readonly IAmazonDynamoDB _dynamoClient;
    private readonly DynamoConfig _dynamoConfig;

    public RoleService(IAmazonDynamoDB dynamoClient, IOptions<DynamoConfig> dynamoConfigOptions)
    {
        _dynamoClient = dynamoClient;
        _dynamoConfig = dynamoConfigOptions.Value;
    }

    public async Task<Role> GetRole(string roleId)
    {
        var role = await _dynamoClient.GetItemAsync(
            _dynamoConfig.RolesTableName,
            new Dictionary<string, AttributeValue>()
            {
                ["id"] = new AttributeValue(roleId)
            });

        return ToRole(role.Item);
    }

    public async Task<Role> SaveRole(Role role)
    {
        var item = new Dictionary<string, AttributeValue>();

        if (string.IsNullOrEmpty(role.Id))
            throw new Exception("Missing role ID.");

        item["id"] = new AttributeValue(role.Id);

        if (!string.IsNullOrEmpty(role.Name))
            item["name"] = new AttributeValue(role.Name);

        if (!string.IsNullOrEmpty(role.Description))
            item["description"] = new AttributeValue(role.Description);

        await _dynamoClient.PutItemAsync(new PutItemRequest()
        {
            TableName = _dynamoConfig.RolesTableName,
            Item = item
        });

        return role;
    }

    public async Task<RolePermission> GetRolePermission(string roleId, string permissionId)
    {
        var response = await _dynamoClient.QueryAsync(
            new QueryRequest()
            {
                TableName = _dynamoConfig.RolePermissionsTableName,
                KeyConditions = new Dictionary<string, Condition>()
                {
                    ["role_id"] = new Condition()
                    {
                        ComparisonOperator = ComparisonOperator.EQ,
                        AttributeValueList = new List<AttributeValue>()
                        {
                            new AttributeValue(roleId)
                        }
                    },
                    ["permission_id"] = new Condition()
                    {
                        ComparisonOperator = ComparisonOperator.EQ,
                        AttributeValueList = new List<AttributeValue>()
                        {
                            new AttributeValue(permissionId)
                        }
                    }
                }
            });

        if (response.Items == default || !response.Items.Any())
        {
            return default;
        }

        return ToRolePermission(response.Items.SingleOrDefault());
    }


    public async Task<IEnumerable<RolePermission>> GetRolePermissions(string roleId)
    {
        var response = await _dynamoClient.QueryAsync(
            new QueryRequest()
            {
                TableName = _dynamoConfig.RolePermissionsTableName,
                KeyConditions = new Dictionary<string, Condition>()
                {
                    ["role_id"] = new Condition()
                    {
                        ComparisonOperator = ComparisonOperator.EQ,
                        AttributeValueList = new List<AttributeValue>()
                        {
                            new AttributeValue(roleId)
                        }
                    }
                }
            });

        if (response.Items == default || !response.Items.Any())
        {
            return default;
        }

        return response.Items.Select(ToRolePermission);
    }

    public async Task<RolePermission> SaveRolePermission(RolePermission rolePermission)
    {
        var item = new Dictionary<string, AttributeValue>();

        if (string.IsNullOrEmpty(rolePermission.RoleId))
            throw new Exception("Missing role ID.");

        item["role_id"] = new AttributeValue(rolePermission.RoleId);

        if (string.IsNullOrEmpty(rolePermission.PermissionId))
            throw new Exception("Missing permission ID.");

        item["permission_id"] = new AttributeValue(rolePermission.PermissionId);

        await _dynamoClient.PutItemAsync(new PutItemRequest()
        {
            TableName = _dynamoConfig.RolePermissionsTableName,
            Item = item
        });

        return rolePermission;
    }


    private Role ToRole(Dictionary<string, AttributeValue> item)
    {
        if (item == default || !item.Any())
            return default;

        return new Role()
        {
            Id = item.ContainsKey("id") ? item["id"].S : default,
            Name = item.ContainsKey("name") ? item["name"].S : default,
            Description = item.ContainsKey("description") ? item["description"].S : default,
        };
    }

    private RolePermission ToRolePermission(Dictionary<string, AttributeValue> item)
    {
        if (item == default || !item.Any())
            return default;

        return new RolePermission()
        {
            RoleId = item.ContainsKey("role_id") ? item["role_id"].S : default,
            PermissionId = item.ContainsKey("permission_id") ? item["permission_id"].S : default,
        };
    }
}
