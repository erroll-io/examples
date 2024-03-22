using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

using Microsoft.Extensions.Options;
using System.Data;

namespace MinimalApi.Services;

public interface IUserRoleService
{
    Task<UserRole> GetUserRole(string userRoleId);
    Task<IEnumerable<UserRole>> GetUserRolesByUserId(string userId);
    Task<UserRole> SaveUserRole(UserRole userRole);
}

public class UserRoleService : IUserRoleService
{
    private readonly IAmazonDynamoDB _dynamoClient;
    private readonly DynamoConfig _dynamoConfig;

    public UserRoleService(IAmazonDynamoDB dynamoClient, IOptions<DynamoConfig> dynamoConfigOptions)
    {
        _dynamoClient = dynamoClient;
        _dynamoConfig = dynamoConfigOptions.Value;
    }

    public async Task<UserRole> GetUserRole(string userRoleId)
    {
        var userRole = await _dynamoClient.GetItemAsync(
            _dynamoConfig.UserRolesTableName,
            new Dictionary<string, AttributeValue>()
            {
                ["id"] = new AttributeValue(userRoleId)
            });

        return ToUserRole(userRole.Item);
    }

    public async Task<IEnumerable<UserRole>> GetUserRolesByUserId(string userId)
    {
        var response = await _dynamoClient.QueryAsync(
            new QueryRequest()
            {
                TableName = _dynamoConfig.UserRolesTableName,
                IndexName = _dynamoConfig.UserRolesTableUserIdIndexName,
                KeyConditions = new Dictionary<string, Condition>()
                {
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

        return response.Items.Select(ToUserRole);
    }

    //public async Task<UserRole> GetUserRoleByUserIdRoleId(string userId, string roleId)
    //{
    //    var response = await _dynamoClient.QueryAsync(
    //        new QueryRequest()
    //        {
    //            TableName = _dynamoConfig.UserRolesTableName,
    //            //IndexName = "index_user_id_role_id",
    //            KeyConditions = new Dictionary<string, Condition>()
    //            {
    //                ["user_id"] = new Condition()
    //                {
    //                    ComparisonOperator = ComparisonOperator.EQ,
    //                    AttributeValueList = new List<AttributeValue>()
    //                    {
    //                        new AttributeValue(userId)
    //                    }
    //                },
    //                ["role_id"] = new Condition()
    //                {
    //                    ComparisonOperator = ComparisonOperator.EQ,
    //                    AttributeValueList = new List<AttributeValue>()
    //                    {
    //                        new AttributeValue(roleId)
    //                    }
    //                }
    //            }
    //        });

    //    if (response.Items == default || !response.Items.Any())
    //    {
    //        return default;
    //    }

    //    return ToUserRole(response.Items.SingleOrDefault());
    //}

    public async Task<UserRole> SaveUserRole(UserRole userRole)
    {
        var item = new Dictionary<string, AttributeValue>();

        if (string.IsNullOrEmpty(userRole.Id))
            throw new Exception("Missing Id.");

        item["id"] = new AttributeValue(userRole.Id);

        if (string.IsNullOrEmpty(userRole.UserId))
            throw new Exception("Missing UserId.");

        item["user_id"] = new AttributeValue(userRole.UserId);

        if (string.IsNullOrEmpty(userRole.RoleId))
            throw new Exception("Missing RoleId.");

        item["role_id"] = new AttributeValue(userRole.RoleId);

        if (!string.IsNullOrEmpty(userRole.Condition))
            item["condition"] = new AttributeValue(userRole.Condition);

        if (userRole.CreatedAt == default)
            throw new Exception("Missing CreatedAt.");

        item["created_at"] = new AttributeValue(userRole.CreatedAt.ToUniversalTime().ToString("o"));

        item["modified_at"] = new AttributeValue(DateTime.UtcNow.ToUniversalTime().ToString("o"));

        await _dynamoClient.PutItemAsync(new PutItemRequest()
        {
            TableName = _dynamoConfig.UserRolesTableName,
            Item = item
        });

        return userRole;
    }

    private UserRole ToUserRole(Dictionary<string, AttributeValue> item)
    {
        if (item == default || !item.Any())
            return default;

        return new UserRole()
        {
            Id = item.ContainsKey("id") ? item["id"].S : default,
            RoleId = item.ContainsKey("role_id") ? item["role_id"].S : default,
            UserId = item.ContainsKey("user_id") ? item["user_id"].S : default,
            Condition = item.ContainsKey("condition") ? item["condition"].S : default,
            CreatedAt = item.ContainsKey("created_at") ? DateTime.Parse(item["created_at"].S) : default,
            ModifiedAt = item.ContainsKey("modified_at") ? DateTime.Parse(item["modified_at"].S) : default,
        };
    }
}
