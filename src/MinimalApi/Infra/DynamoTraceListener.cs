//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Net;
//
//using Amazon.DynamoDBv2;
//using Amazon.DynamoDBv2.Model;
//using Microsoft.Extensions.Options;
//
//namespace MinimalApi;
//
//public class DynamoTraceListener : TraceListener
//{
//    private readonly IAmazonDynamoDB _dynamoDbClient;
//    private readonly DynamoConfig _dynamoConfig;
//
//    public DynamoTraceListener(
//        IAmazonDynamoDB dynamoDbClient,
//        IOptions<DynamoConfig> dynamoConfigOptions)
//        : base()
//    {
//        _dynamoDbClient = dynamoDbClient;
//        _dynamoConfig = dynamoConfigOptions.Value;
//    }
//
//    public DynamoTraceListener(string name)
//        : base(name)
//    {
//    }
//
//    public override async void Write(string? message)
//    {
//        var response = await _dynamoDbClient.PutItemAsync(new PutItemRequest()
//        {
//            TableName = _dynamoConfig.TracesTableName,
//            Item = new Dictionary<string, AttributeValue>()
//            {
//                ["id"] = new AttributeValue(Guid.NewGuid().ToString()),
//                ["created_at"] = new AttributeValue(DateTime.UtcNow.ToUniversalTime().ToString("o")),
//                ["value"] = new AttributeValue(message)
//            }
//        });
//
//        if (response.HttpStatusCode != HttpStatusCode.OK)
//            throw new Exception("Failed to write trace.");
//    }
//
//    public override void WriteLine(string? message)
//    {
//        Write(message);
//    }
//}
//