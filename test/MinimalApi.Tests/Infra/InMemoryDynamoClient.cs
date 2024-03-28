using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using LinqKit;

namespace MinimalApi.Tests;

public class InMemoryDynamoClient : IAmazonDynamoDB
{
        public IDynamoDBv2PaginatorFactory Paginators { get; }
        public IClientConfig Config { get; }

        private static Dictionary<string, Dictionary<string, Dictionary<string, AttributeValue>>> _data;

        private Dictionary<string, string> _hashKeyAttributesByTableName;
        private Dictionary<string, string> _rangeKeyAttributesByTableName;

        // TODO: flag to modulate between static vs. instance backing store
        public InMemoryDynamoClient(
            Dictionary<string, string> hashKeyAttributesByTableName,
            Dictionary<string, string> rangeKeyAttributesByTableName = default,
            Action<Dictionary<string, Dictionary<string, Dictionary<string, AttributeValue>>>> initializer = default)
        {
            _hashKeyAttributesByTableName = hashKeyAttributesByTableName;
            _rangeKeyAttributesByTableName = rangeKeyAttributesByTableName;

            if (_data == default)
            {
                _data = new Dictionary<string, Dictionary<string, Dictionary<string, AttributeValue>>>();

                initializer?.Invoke(_data);
            }
        }

        public Task<BatchGetItemResponse> BatchGetItemAsync(
            BatchGetItemRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new BatchGetItemResponse()
                {
                    Responses = request.RequestItems
                        .Select(record => 
                        {
                            var tableName = record.Key;
                            var hashKeyName = _hashKeyAttributesByTableName[tableName];

                            return new 
                            {
                                TableName = tableName,
                                Responses = record.Value.Keys.Select(key => _data[tableName][key[hashKeyName].S])
                            };
                        })
                        .ToDictionary(kv => kv.TableName, kv => kv.Responses.ToList())
                });
        }

        public Task<GetItemResponse> GetItemAsync(
            GetItemRequest request,
            CancellationToken cancellationToken = default)
        {
            var hashKeyName = _hashKeyAttributesByTableName[request.TableName];
            var hashKey = request.Key[hashKeyName].S;

            var response = new GetItemResponse();

            if (_data.ContainsKey(request.TableName) && _data[request.TableName].ContainsKey(hashKey))
            {
                response.IsItemSet = true;
                response.Item = _data[request.TableName][hashKey];
            }

            return Task.FromResult(response);
        }

        public Task<GetItemResponse> GetItemAsync(
            string tableName,
            Dictionary<string, AttributeValue> key,
            CancellationToken cancellationToken = default)
        {
            return GetItemAsync(new GetItemRequest()
            {
                TableName = tableName,
                Key = key
            });
        }

        public Task<PutItemResponse> PutItemAsync(
            PutItemRequest request,
            CancellationToken cancellationToken = default)
        {
            var hashKeyName = _hashKeyAttributesByTableName[request.TableName];
            var rangeKeyName = _rangeKeyAttributesByTableName != default
                && _rangeKeyAttributesByTableName.ContainsKey(request.TableName)
                    ? _rangeKeyAttributesByTableName[request.TableName]
                    : string.Empty;
            var hashKeyValue = string.IsNullOrEmpty(rangeKeyName)
                ? request.Item[hashKeyName].S
                : $"{request.Item[hashKeyName].S}+{request.Item[rangeKeyName].S}";

            if (!_data.ContainsKey(request.TableName))
            {
                _data[request.TableName] = new Dictionary<string, Dictionary<string, AttributeValue>>();
            }

            _data[request.TableName][hashKeyValue] = request.Item;

            return Task.FromResult(new PutItemResponse());
        }

        public Task<QueryResponse> QueryAsync(QueryRequest request, CancellationToken cancellationToken = default)
        {
            var predicate = PredicateBuilder.New<Dictionary<string, AttributeValue>>(true);

            foreach (var keyCondition in request.KeyConditions)
            {
                Func<string, string, bool> comparer = default; 
                
                if (keyCondition.Value.ComparisonOperator == ComparisonOperator.EQ)
                {
                    comparer = (a, b) => a == b;
                }
                else if (keyCondition.Value.ComparisonOperator == ComparisonOperator.BEGINS_WITH)
                {
                    comparer = (a, b) => a.StartsWith(b);
                }
                else
                {
                    throw new NotImplementedException(
                        $"Operator {keyCondition.Value.ComparisonOperator} is not implemented");
                }

                predicate = predicate.And(record =>
                    comparer(record[keyCondition.Key].S, keyCondition.Value.AttributeValueList.First().S));
            }

            var response = new QueryResponse();

            if (_data.ContainsKey(request.TableName))
                response.Items = _data[request.TableName].Values.AsQueryable().Where(predicate).ToList();

            return Task.FromResult(response);
        }

        public Task<BatchExecuteStatementResponse> BatchExecuteStatementAsync(BatchExecuteStatementRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<BatchGetItemResponse> BatchGetItemAsync(Dictionary<string, KeysAndAttributes> requestItems, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<BatchGetItemResponse> BatchGetItemAsync(Dictionary<string, KeysAndAttributes> requestItems, ReturnConsumedCapacity returnConsumedCapacity, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<BatchWriteItemResponse> BatchWriteItemAsync(Dictionary<string, List<WriteRequest>> requestItems, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<BatchWriteItemResponse> BatchWriteItemAsync(BatchWriteItemRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<CreateBackupResponse> CreateBackupAsync(CreateBackupRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<CreateGlobalTableResponse> CreateGlobalTableAsync(CreateGlobalTableRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<CreateTableResponse> CreateTableAsync(string tableName, List<KeySchemaElement> keySchema, List<AttributeDefinition> attributeDefinitions, ProvisionedThroughput provisionedThroughput, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<CreateTableResponse> CreateTableAsync(CreateTableRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<DeleteBackupResponse> DeleteBackupAsync(DeleteBackupRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<DeleteItemResponse> DeleteItemAsync(string tableName, Dictionary<string, AttributeValue> key, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<DeleteItemResponse> DeleteItemAsync(string tableName, Dictionary<string, AttributeValue> key, ReturnValue returnValues, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<DeleteItemResponse> DeleteItemAsync(DeleteItemRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<DeleteTableResponse> DeleteTableAsync(string tableName, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<DeleteTableResponse> DeleteTableAsync(DeleteTableRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<DescribeBackupResponse> DescribeBackupAsync(DescribeBackupRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<DescribeContinuousBackupsResponse> DescribeContinuousBackupsAsync(DescribeContinuousBackupsRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<DescribeContributorInsightsResponse> DescribeContributorInsightsAsync(DescribeContributorInsightsRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<DescribeEndpointsResponse> DescribeEndpointsAsync(DescribeEndpointsRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<DescribeExportResponse> DescribeExportAsync(DescribeExportRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<DescribeGlobalTableResponse> DescribeGlobalTableAsync(DescribeGlobalTableRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<DescribeGlobalTableSettingsResponse> DescribeGlobalTableSettingsAsync(DescribeGlobalTableSettingsRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<DescribeImportResponse> DescribeImportAsync(DescribeImportRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<DescribeKinesisStreamingDestinationResponse> DescribeKinesisStreamingDestinationAsync(DescribeKinesisStreamingDestinationRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<DescribeLimitsResponse> DescribeLimitsAsync(DescribeLimitsRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<DescribeTableResponse> DescribeTableAsync(string tableName, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<DescribeTableResponse> DescribeTableAsync(DescribeTableRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<DescribeTableReplicaAutoScalingResponse> DescribeTableReplicaAutoScalingAsync(DescribeTableReplicaAutoScalingRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<DescribeTimeToLiveResponse> DescribeTimeToLiveAsync(string tableName, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<DescribeTimeToLiveResponse> DescribeTimeToLiveAsync(DescribeTimeToLiveRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Amazon.Runtime.Endpoints.Endpoint DetermineServiceOperationEndpoint(AmazonWebServiceRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<DisableKinesisStreamingDestinationResponse> DisableKinesisStreamingDestinationAsync(DisableKinesisStreamingDestinationRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<EnableKinesisStreamingDestinationResponse> EnableKinesisStreamingDestinationAsync(EnableKinesisStreamingDestinationRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ExecuteStatementResponse> ExecuteStatementAsync(ExecuteStatementRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ExecuteTransactionResponse> ExecuteTransactionAsync(ExecuteTransactionRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ExportTableToPointInTimeResponse> ExportTableToPointInTimeAsync(ExportTableToPointInTimeRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<GetItemResponse> GetItemAsync(string tableName, Dictionary<string, AttributeValue> key, bool consistentRead, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ImportTableResponse> ImportTableAsync(ImportTableRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ListBackupsResponse> ListBackupsAsync(ListBackupsRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ListContributorInsightsResponse> ListContributorInsightsAsync(ListContributorInsightsRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ListExportsResponse> ListExportsAsync(ListExportsRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ListGlobalTablesResponse> ListGlobalTablesAsync(ListGlobalTablesRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ListImportsResponse> ListImportsAsync(ListImportsRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ListTablesResponse> ListTablesAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ListTablesResponse> ListTablesAsync(string exclusiveStartTableName, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ListTablesResponse> ListTablesAsync(string exclusiveStartTableName, int limit, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ListTablesResponse> ListTablesAsync(int limit, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ListTablesResponse> ListTablesAsync(ListTablesRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ListTagsOfResourceResponse> ListTagsOfResourceAsync(ListTagsOfResourceRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<PutItemResponse> PutItemAsync(string tableName, Dictionary<string, AttributeValue> item, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<PutItemResponse> PutItemAsync(string tableName, Dictionary<string, AttributeValue> item, ReturnValue returnValues, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<RestoreTableFromBackupResponse> RestoreTableFromBackupAsync(RestoreTableFromBackupRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<RestoreTableToPointInTimeResponse> RestoreTableToPointInTimeAsync(RestoreTableToPointInTimeRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ScanResponse> ScanAsync(ScanRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ScanResponse> ScanAsync(string tableName, List<string> attributesToGet, Dictionary<string, Condition> scanFilter, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ScanResponse> ScanAsync(string tableName, Dictionary<string, Condition> scanFilter, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ScanResponse> ScanAsync(string tableName, List<string> attributesToGet, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<TagResourceResponse> TagResourceAsync(TagResourceRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<TransactGetItemsResponse> TransactGetItemsAsync(TransactGetItemsRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<TransactWriteItemsResponse> TransactWriteItemsAsync(TransactWriteItemsRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<UntagResourceResponse> UntagResourceAsync(UntagResourceRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<UpdateContinuousBackupsResponse> UpdateContinuousBackupsAsync(UpdateContinuousBackupsRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<UpdateContributorInsightsResponse> UpdateContributorInsightsAsync(UpdateContributorInsightsRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<UpdateGlobalTableResponse> UpdateGlobalTableAsync(UpdateGlobalTableRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<UpdateGlobalTableSettingsResponse> UpdateGlobalTableSettingsAsync(UpdateGlobalTableSettingsRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<UpdateItemResponse> UpdateItemAsync(string tableName, Dictionary<string, AttributeValue> key, Dictionary<string, AttributeValueUpdate> attributeUpdates, ReturnValue returnValues, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<UpdateItemResponse> UpdateItemAsync(UpdateItemRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<UpdateItemResponse> UpdateItemAsync(string tableName, Dictionary<string, AttributeValue> key, Dictionary<string, AttributeValueUpdate> attributeUpdates, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<UpdateTableResponse> UpdateTableAsync(string tableName, ProvisionedThroughput provisionedThroughput, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<UpdateTableResponse> UpdateTableAsync(UpdateTableRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<UpdateTableReplicaAutoScalingResponse> UpdateTableReplicaAutoScalingAsync(UpdateTableReplicaAutoScalingRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<UpdateTimeToLiveResponse> UpdateTimeToLiveAsync(UpdateTimeToLiveRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
        }
}
