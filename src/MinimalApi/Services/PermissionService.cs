using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

using Microsoft.Extensions.Options;

namespace MinimalApi.Services;

public interface IPermissionService
{
    Task<Permission> GetPermission(string permissionId);
    Task<Permission> SavePermission(Permission permission);
}

public class PermissionService : IPermissionService
{
    private readonly IAmazonDynamoDB _dynamoClient;
    private readonly DynamoConfig _dynamoConfig;

    public PermissionService(IAmazonDynamoDB dynamoClient, IOptions<DynamoConfig> dynamoConfigOptions)
    {
        _dynamoClient = dynamoClient;
        _dynamoConfig = dynamoConfigOptions.Value;
    }

    public async Task<Permission> GetPermission(string permissionId)
    {
        var permission = await _dynamoClient.GetItemAsync(
            _dynamoConfig.PermissionsTableName,
            new Dictionary<string, AttributeValue>()
            {
                ["id"] = new AttributeValue(permissionId)
            });

        return ToPermission(permission.Item);
    }

    public async Task<Permission> SavePermission(Permission permission)
    {
        var item = new Dictionary<string, AttributeValue>();

        if (string.IsNullOrEmpty(permission.Id))
            throw new Exception("Missing permission ID.");

        item["id"] = new AttributeValue(permission.Id);

        if (!string.IsNullOrEmpty(permission.Name))
            item["name"] = new AttributeValue(permission.Name);

        if (!string.IsNullOrEmpty(permission.Description))
            item["description"] = new AttributeValue(permission.Description);


        await _dynamoClient.PutItemAsync(new PutItemRequest()
        {
            TableName = _dynamoConfig.PermissionsTableName,
            Item = item
        });

        return permission;
    }

    private Permission ToPermission(Dictionary<string, AttributeValue> item)
    {
        if (item == default || !item.Any())
            return default;

        return new Permission()
        {
            Id = item.ContainsKey("id") ? item["id"].S : default,
            Name = item.ContainsKey("name") ? item["name"].S : default,
            Description = item.ContainsKey("description") ? item["description"].S : default,
        };
    }
}
