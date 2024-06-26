using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MinimalApi.Services;

public interface IUserService
{
    Task<User> CreateUser(UserCreateParams userCreateParams);
    Task<User> GetUser(string id);
    Task<ServiceResult<User>> GetCurrentUser(ClaimsPrincipal principal);
    Task<string> GetCurrentUserId(ClaimsPrincipal principal);
    Task<User> SaveUser(User user);
    Task UpdateUser(string id, UserCreateParams updateParams);
    Task UpdateCurrentUser(ClaimsPrincipal principal, UserCreateParams updateParams);
}

public class UserService : IUserService
{
    private readonly ILogger _logger;
    private readonly IDistributedCache _cache;
    private readonly IAmazonDynamoDB _dynamoClient;
    private readonly IHasher _hasher;
    private readonly DynamoConfig _dynamoConfig;

    public UserService(
        ILogger<UserService> logger,
        IAmazonDynamoDB dynamoClient,
        IHasher hasher,
        IOptions<DynamoConfig> dynamoConfigOptions,
        IDistributedCache cache = default)
    {
        _logger = logger;
        _cache = cache;
        _dynamoClient = dynamoClient;
        _hasher = hasher;
        _dynamoConfig = dynamoConfigOptions.Value;
        _cache = cache;
    }

    public async Task<User> CreateUser(UserCreateParams userCreateParams)
    {
        var emailHash = string.Empty;

        // TODO: for now we'll consider email optional; revisit later
        if (!string.IsNullOrEmpty(userCreateParams.Email))
        {
            emailHash = _hasher.Hash(userCreateParams.Email);

            var existingUser = await GetUserByEmailHash(emailHash);
            if (existingUser != default)
                throw new Exception("conflict.");
        }

        var now = DateTime.UtcNow;

        return await SaveUser(
            new User()
            {
                Id = Guid.NewGuid().ToString(),
                PrincipalId = userCreateParams.PrincipalId,
                EmailHash = emailHash,
                Language = userCreateParams.Language,
                Timezone = userCreateParams.Timezone,
                Metadata = userCreateParams.Metadata,
                CreatedAt = now,
                ModifiedAt = now
            });
    }

    public async Task<User> GetUser(string id)
    {
        var user = await _dynamoClient.GetItemAsync(
            _dynamoConfig.UsersTableName,
            new Dictionary<string, AttributeValue>()
            {
                ["id"] = new AttributeValue(id)
            });

        return ToUser(user.Item);
    }

    public Task<User> GetUser(ClaimsPrincipal principal)
    {
        var principalId = principal.Claims.FirstOrDefault(claim => claim.Type == "sub")?.Value;

        if (string.IsNullOrEmpty(principalId))
        {
            throw new Exception("Forbidden");
        }

        return GetUserByPrincipalId(principalId);
    }

    public async Task<ServiceResult<User>> GetCurrentUser(ClaimsPrincipal principal)
    {
        var principalId = ClaimsPrincipalLogic.GetPrincipalIdentity(principal, out var claimType);

        if (string.IsNullOrEmpty(principalId) || string.IsNullOrEmpty(claimType))
            return ServiceResult<User>.Forbidden();

        var user = claimType == "sub" ? await GetUser(principal) : await GetUser(principalId);

        if (user == default)
        {
            user = await CreateUser(
                new UserCreateParams
                {
                    PrincipalId = principalId
                });
        }
        else
        {
            if (string.IsNullOrEmpty(user.PrincipalId))
            {
                user.PrincipalId = principalId;

                await SaveUser(user);
            }
        }
        
        return ServiceResult<User>.Success(user);
    }

    public async Task<string> GetCurrentUserId(ClaimsPrincipal principal)
    {
        var sub = principal.GetSub();
        var userId = string.Empty;

        if (string.IsNullOrEmpty(sub))
            return string.Empty;

        if (_cache != default)
        {
            userId = await _cache.Get<string>(sub);

            if (string.IsNullOrEmpty(userId))
            {
                var userResult = await GetCurrentUser(principal);
                userId = userResult.Result.Id;

                await _cache.Set(sub, userId);
            }
            else
            {
                //_logger.LogInformation($"Using cached userId for {sub}.");
            }
        }
        else
        {
            var userResult = await GetCurrentUser(principal);
            userId = userResult.Result.Id;
        }

        return userId;
    }

    public async Task UpdateUser(string id, UserCreateParams updateParams)
    {
        var user = await GetUserByPrincipalId(id);

        if (user == default)
            throw new Exception("forbid");

        var doSave = false;
        
        if (!string.IsNullOrEmpty(updateParams.Language))
        {
            user.Language = updateParams.Language;
            doSave = true;
        }
        if (!string.IsNullOrEmpty(updateParams.Timezone))
        {
            user.Timezone = updateParams.Timezone;
            doSave = true;
        }
        if (!string.IsNullOrEmpty(updateParams.Metadata))
        {
            user.Metadata = updateParams.Metadata;
            doSave = true;
        }

        if (!doSave)
            return;

        user.ModifiedAt = DateTime.UtcNow;

        await SaveUser(user);   
    }

    public Task UpdateCurrentUser(ClaimsPrincipal principal, UserCreateParams updateParams)
    {
        return UpdateUser(principal.GetPrincipalIdentity(), updateParams);
    }

    private async Task<User> GetUserByPrincipalId(string principalId)
    {
        var response = await _dynamoClient.QueryAsync(
            new QueryRequest()
            {
                TableName = _dynamoConfig.UsersTableName,
                IndexName = "index_principal_id",
                KeyConditions = new Dictionary<string, Condition>()
                {
                    ["principal_id"] = new Condition()
                    {
                        ComparisonOperator = ComparisonOperator.EQ,
                        AttributeValueList = new List<AttributeValue>()
                        {
                            new AttributeValue(principalId)
                        }
                    }
                }
            });

        if (response.Items == default || !response.Items.Any())
        {
            return default;
        }

        return ToUser(response.Items.SingleOrDefault());
    }

    private async Task<User> GetUserByEmailHash(string emailHash)
    {
        var response = await _dynamoClient.QueryAsync(
            new QueryRequest()
            {
                TableName = _dynamoConfig.UsersTableName,
                IndexName = "index_email_hash",
                KeyConditions = new Dictionary<string, Condition>()
                {
                    ["email_hash"] = new Condition()
                    {
                        ComparisonOperator = ComparisonOperator.EQ,
                        AttributeValueList = new List<AttributeValue>()
                        {
                            new AttributeValue(emailHash)
                        }
                    }
                }
            });

        if (response.Items == default || !response.Items.Any())
        {
            return default;
        }

        return ToUser(response.Items.SingleOrDefault());
    }

    public async Task<User> SaveUser(User user)
    {
        await _dynamoClient.PutItemAsync(new PutItemRequest()
        {
            TableName = _dynamoConfig.UsersTableName,
            Item = FromUser(user)
        });

        return user;
    }

    internal static Dictionary<string, AttributeValue> FromUser(User user)
    {
        var item = new Dictionary<string, AttributeValue>();

        if (string.IsNullOrEmpty(user.Id))
            throw new Exception("Missing user ID.");

        item["id"] = new AttributeValue(user.Id);

        if (!string.IsNullOrEmpty(user.PrincipalId))
            item["principal_id"] = new AttributeValue(user.PrincipalId);

        if (!string.IsNullOrEmpty(user.EmailHash))
            item["email_hash"] = new AttributeValue(user.EmailHash);

        if (!string.IsNullOrEmpty(user.Language))
            item["language"] = new AttributeValue(user.Language);

        if (!string.IsNullOrEmpty(user.Timezone))
            item["timezone"] = new AttributeValue(user.Timezone);

        if (!string.IsNullOrEmpty(user.Metadata))
            item["metadata"] = new AttributeValue(user.Metadata);

        if (user.CreatedAt != default)
            item["created_at"] = new AttributeValue(user.CreatedAt.ToUniversalTime().ToString("o"));

        item["modified_at"] = new AttributeValue(DateTime.UtcNow.ToUniversalTime().ToString("o"));

        return item;
    }

    internal static User ToUser(Dictionary<string, AttributeValue> item)
    {
        // TODO: review this
        if (item.ContainsKey("deleted_at"))
            throw new Exception("not found");

        if (!item.Any())
            return default;

        return new User()
        {
            Id = item.ContainsKey("id") ? item["id"].S : default,
            PrincipalId = item.ContainsKey("principal_id") ? item["principal_id"].S : default,
            Name = item.ContainsKey("name") ? item["name"].S : default,
            Timezone = item.ContainsKey("timezone") ? item["timezone"].S : default,
            Language = item.ContainsKey("language") ? item["language"].S : default,
            Metadata = item.ContainsKey("metadata") ? item["metadata"].S : default,
            CreatedAt = item.ContainsKey("created_at") ? DateTime.Parse(item["created_at"].S) : default,
            ModifiedAt = item.ContainsKey("modified_at") ? DateTime.Parse(item["modified_at"].S) : default,
        };
    }
}
