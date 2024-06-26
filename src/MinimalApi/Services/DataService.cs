using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.AspNetCore.Authorization; 

using Microsoft.Extensions.Options;
using System.Data;

namespace MinimalApi.Services;

public interface IDataService
{
    Task<ServiceResult<DataRecord>> CreateDataRecord(ClaimsPrincipal principal, DataRecordParams dataRecordParams);
    Task<ServiceResult<DataRecord>> CreateProjectData(
        ClaimsPrincipal principal,
        string projectId,
        DataRecordParams dataRecordParams);
    Task<ServiceResult<DataRecord>> FinalizeDataUpload(
        ClaimsPrincipal principal,
        string dataRecordId,
        IEnumerable<string> partTags);
    Task<ServiceResult<DataRecord>> GetDataRecord(ClaimsPrincipal principal, string dataRecordId);
    Task<ServiceResult<IEnumerable<DataRecord>>> GetUserData(ClaimsPrincipal principal);
    Task<ServiceResult<IEnumerable<DataRecord>>> GetProjectData(ClaimsPrincipal principal, string projectId);
    Task<DataRecord> SaveDataRecord(DataRecord dataRecord);
    Task<ProjectData> GetProjectData(string projectId, string dataRecordId);
    Task<ProjectData> SaveProjectData(ProjectData projectData);
}

public class DataService : IDataService
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IAmazonDynamoDB _dynamoClient;
    private readonly IUserRoleService _userRoleService;
    private readonly IUserService _userService;
    private readonly DynamoConfig _dynamoConfig;

    public DataService(
        IAuthorizationService authorizationService,
        IAmazonDynamoDB dynamoClient,
        IUserRoleService userRoleService,
        IUserService userService,
        IOptions<DynamoConfig> dynamoConfigOptions)
    {
        _authorizationService = authorizationService;
        _dynamoClient = dynamoClient;
        _userRoleService = userRoleService;
        _userService = userService;
        _dynamoConfig = dynamoConfigOptions.Value;
    }

    public async Task<ServiceResult<DataRecord>> CreateDataRecord(
        ClaimsPrincipal principal,
        DataRecordParams dataRecordParams)
    {
        if (principal == default || string.IsNullOrEmpty(principal.GetPrincipalIdentity()))
        {
            return ServiceResult<DataRecord>.Forbidden(AuthorizationResult.Failed());
        }

        var userResult = await _userService.GetCurrentUser(principal);

        // TODO: transaction
        {
            var dataRecord = new DataRecord()
            {
                Id = Guid.NewGuid().ToString(),
                DataTypeId = dataRecordParams.DataTypeId,
                Location = $"s3://TODO",
                FileName = dataRecordParams.FileName,
                Size = dataRecordParams.Size,
                CreatedAt = DateTime.UtcNow
            };

            await SaveDataRecord(dataRecord);

            await _userRoleService.CreateUserRole(
                userResult.Result.Id,
                "MinimalApi::Role::DataOwner",
                $"MinimalApi::DataRecord:{dataRecord.Id}");

            return ServiceResult<DataRecord>.Success(dataRecord);
        }
    }

    public async Task<ServiceResult<DataRecord>> CreateProjectData(
        ClaimsPrincipal principal,
        string projectId,
        DataRecordParams dataRecordParams)
    {
        var user = await _userService.GetCurrentUser(principal);

        var authorizationResult = await _authorizationService.AuthorizeAsync(
            principal,
            new OperationRequirement(
                "MinimalApi::Action::CreateProjectData",
                $"MinimalApi::Project::{projectId}"));

        if (!authorizationResult.Succeeded)
            return ServiceResult<DataRecord>.Forbidden(AuthorizationResult.Failed());

        // TODO: transaction
        {
            var dataRecordResult = await CreateDataRecord(principal, dataRecordParams);

            if (!dataRecordResult.IsSuccess)
            {
                return dataRecordResult;
            }

            var projectData = new ProjectData()
            {
                ProjectId = projectId,
                DataRecordId = dataRecordResult.Result.Id,
                CreatedAt = DateTime.UtcNow
            };

            await SaveProjectData(projectData);

            return ServiceResult<DataRecord>.Success(dataRecordResult.Result);
        }
    }

    public Task<ServiceResult<DataRecord>> FinalizeDataUpload(
        ClaimsPrincipal principal,
        string dataRecordId,
        IEnumerable<string> partTags)
    {
        throw new NotImplementedException();
    }

    public async Task<ServiceResult<DataRecord>> GetDataRecord(ClaimsPrincipal principal, string dataRecordId)
    {
        var authorizationResult = await _authorizationService.AuthorizeAsync(
            principal,
            new OperationRequirement(
                "MinimalApi::Action::ReadData",
                $"MinimalApi::DataRecord:{dataRecordId}"));

        if (!authorizationResult.Succeeded)
            return ServiceResult<DataRecord>.Forbidden(AuthorizationResult.Failed());

        var dataRecord = await GetDataRecord(dataRecordId);

        return dataRecord == default
            ? ServiceResult<DataRecord>.Failure()
            : ServiceResult<DataRecord>.Success(dataRecord);
    }

    internal async Task<DataRecord> GetDataRecord(string dataRecordId)
    {
        var data = await _dynamoClient.GetItemAsync(
            _dynamoConfig.DataTableName,
            new Dictionary<string, AttributeValue>()
            {
                ["id"] = new AttributeValue(dataRecordId)
            });

        return ToDataRecord(data.Item);
    }

    public async Task<ServiceResult<IEnumerable<DataRecord>>> GetUserData(ClaimsPrincipal principal)
    {
        var user = await _userService.GetCurrentUser(principal);

        if (user == default)
        {
            return ServiceResult<IEnumerable<DataRecord>>.Forbidden(AuthorizationResult.Failed());
        }

        var dataRecordIds = await _userRoleService.GetUserRoleConditionValues(
            principal,
            "MinimalApi::Action::ReadData",
            "MinimalApi::DataRecord");

        if (dataRecordIds == default || !dataRecordIds.Any())
        {
            return ServiceResult<IEnumerable<DataRecord>>.Success(Enumerable.Empty<DataRecord>());
        }

        var dataResponse = await _dynamoClient.BatchGetItemAsync(
            new BatchGetItemRequest()
            {
                RequestItems  = new Dictionary<string, KeysAndAttributes>()
                {
                    [_dynamoConfig.DataTableName] = new KeysAndAttributes()
                    {
                        Keys = dataRecordIds.Select(dataRecordId =>
                            new Dictionary<string, AttributeValue>()
                            {
                                ["id"] = new AttributeValue(dataRecordId)
                            }).ToList()
                    }
                }
            });

        return ServiceResult<IEnumerable<DataRecord>>.Success(
            dataResponse.Responses
                .SelectMany(response => response.Value)
                .Select(response => ToDataRecord(response)));
    }

    public async Task<ServiceResult<IEnumerable<DataRecord>>> GetProjectData(
        ClaimsPrincipal principal,
        string projectId)
    {
        var authorizationResult = await _authorizationService.AuthorizeAsync(
            principal,
            new OperationRequirement(
                "MinimalApi::Action::ReadProjectData",
                $"MinimalApi::Project::{projectId}"));

        if (!authorizationResult.Succeeded)
            return ServiceResult<IEnumerable<DataRecord>>.Forbidden(AuthorizationResult.Failed());

        var user = await _userService.GetCurrentUser(principal);

        if (user == default)
        {
            return ServiceResult<IEnumerable<DataRecord>>.Forbidden(AuthorizationResult.Failed());
        }

        var projectDataResponse = await _dynamoClient.QueryAsync(
            new QueryRequest()
            {
                TableName = _dynamoConfig.ProjectDataTableName,
                KeyConditions = new Dictionary<string, Condition>()
                {
                    ["project_id"] = new Condition()
                    {
                        ComparisonOperator = ComparisonOperator.EQ,
                        AttributeValueList = new List<AttributeValue>()
                        {
                            new AttributeValue(projectId)
                        }
                    }
                },
                ProjectionExpression = "data_record_id"
            });

        if (projectDataResponse.Items == default || !projectDataResponse.Items.Any())
        {
            return ServiceResult<IEnumerable<DataRecord>>.Success(Enumerable.Empty<DataRecord>());
        }

        var dataRecordIds = projectDataResponse.Items.Select(item => item["data_record_id"]?.S);

        var dataResponse = await _dynamoClient.BatchGetItemAsync(
            new BatchGetItemRequest()
            {
                RequestItems  = new Dictionary<string, KeysAndAttributes>()
                {
                    [_dynamoConfig.DataTableName] = new KeysAndAttributes()
                    {
                        Keys = dataRecordIds.Select(dataRecordId =>
                            new Dictionary<string, AttributeValue>()
                            {
                                ["id"] = new AttributeValue(dataRecordId)
                            }).ToList()
                    }
                }
            });

        return ServiceResult<IEnumerable<DataRecord>>.Success(
            dataResponse.Responses
                .SelectMany(response => response.Value)
                .Select(response => ToDataRecord(response)));
    }

    public async Task<ProjectData> GetProjectData(string projectId, string dataRecordId)
    {
        var projectDataResponse = await _dynamoClient.GetItemAsync(
            _dynamoConfig.ProjectDataTableName,
            new Dictionary<string, AttributeValue>()
            {
                ["project_id"] = new AttributeValue(projectId),
                ["data_record_id"] = new AttributeValue(dataRecordId),
            });

        return ToProjectData(projectDataResponse.Item);
    }

    public async Task<DataRecord> SaveDataRecord(DataRecord dataRecord)
    {
        var item = new Dictionary<string, AttributeValue>();

        if (string.IsNullOrEmpty(dataRecord.Id))
            throw new Exception("Missing ID.");

        item["id"] = new AttributeValue(dataRecord.Id);

        if (string.IsNullOrEmpty(dataRecord.DataTypeId))
            throw new Exception("Missing data type ID.");

        item["data_type_id"] = new AttributeValue(dataRecord.DataTypeId);

        if (string.IsNullOrEmpty(dataRecord.FileName))
            throw new Exception("Missing file name.");

        item["file_name"] = new AttributeValue(dataRecord.FileName);

        if (string.IsNullOrEmpty(dataRecord.Location))
            throw new Exception("Missing location.");

        item["location"] = new AttributeValue(dataRecord.Location);

        if (dataRecord.Size == default)
            throw new Exception("Missing size.");

        item["size"] = new AttributeValue() { N = dataRecord.Size.ToString() };

        if (dataRecord.CreatedAt == default)
            throw new Exception("Missing CreatedAt.");

        if (!string.IsNullOrEmpty(dataRecord.Metadata))
            item["metadata"] = new AttributeValue(dataRecord.Metadata);

        if (dataRecord.CreatedAt == default)
            throw new Exception("Missing CreatedAt.");

        item["created_at"] = new AttributeValue(dataRecord.CreatedAt.ToUniversalTime().ToString("o"));

        item["modified_at"] = new AttributeValue(DateTime.UtcNow.ToUniversalTime().ToString("o"));

        await _dynamoClient.PutItemAsync(new PutItemRequest()
        {
            TableName = _dynamoConfig.DataTableName,
            Item = item
        });

        return dataRecord;
    }

    public async Task<ProjectData> SaveProjectData(ProjectData projectData)
    {
        var item = new Dictionary<string, AttributeValue>();

        if (string.IsNullOrEmpty(projectData.ProjectId))
            throw new Exception("Missing Project ID.");

        item["project_id"] = new AttributeValue(projectData.ProjectId);

        if (string.IsNullOrEmpty(projectData.DataRecordId))
            throw new Exception("Missing data type ID.");

        item["data_record_id"] = new AttributeValue(projectData.DataRecordId);

        if (!string.IsNullOrEmpty(projectData.Metadata))
            item["metadata"] = new AttributeValue(projectData.Metadata);

        if (projectData.CreatedAt == default)
            throw new Exception("Missing CreatedAt.");

        item["created_at"] = new AttributeValue(projectData.CreatedAt.ToUniversalTime().ToString("o"));

        item["modified_at"] = new AttributeValue(DateTime.UtcNow.ToUniversalTime().ToString("o"));

        await _dynamoClient.PutItemAsync(new PutItemRequest()
        {
            TableName = _dynamoConfig.ProjectDataTableName,
            Item = item
        });

        return projectData;
    }

    private DataRecord ToDataRecord(Dictionary<string, AttributeValue> item)
    {
        if (item == default || !item.Any())
            return default;

        return new DataRecord()
        {
            Id = item.ContainsKey("id") ? item["id"].S : default,
            DataTypeId = item.ContainsKey("data_type_id") ? item["data_type_id"].S : default,
            FileName = item.ContainsKey("file_name") ? item["file_name"].S : default,
            Location = item.ContainsKey("location") ? item["location"].S : default,
            Size = item.ContainsKey("size") ? ulong.Parse(item["size"].N) : default,
            Metadata = item.ContainsKey("metadata") ? item["metadata"].S : default,
            CreatedAt = item.ContainsKey("created_at") ? DateTime.Parse(item["created_at"].S) : default,
            ModifiedAt = item.ContainsKey("modified_at") ? DateTime.Parse(item["modified_at"].S) : default
        };
    }

    private ProjectData ToProjectData(Dictionary<string, AttributeValue> item)
    {
        if (item == default || !item.Any())
            return default;

        return new ProjectData()
        {
            ProjectId = item.ContainsKey("project_id") ? item["project_id"].S : default,
            DataRecordId = item.ContainsKey("data_record_id") ? item["data_record_id"].S : default,
            Metadata = item.ContainsKey("metadata") ? item["metadata"].S : default,
            CreatedAt = item.ContainsKey("created_at") ? DateTime.Parse(item["created_at"].S) : default,
            ModifiedAt = item.ContainsKey("modified_at") ? DateTime.Parse(item["modified_at"].S) : default
        };
    }
}
