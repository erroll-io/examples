using System.Text.Json.Serialization;

using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;

namespace MinimalApi;

[JsonSerializable(typeof(ILambdaContext))]
[JsonSerializable(typeof(APIGatewayHttpApiV2ProxyRequest))]
[JsonSerializable(typeof(APIGatewayHttpApiV2ProxyResponse))]
[JsonSerializable(typeof(HealthRequest))]
[JsonSerializable(typeof(HealthResponse))]
[JsonSerializable(typeof(UserCreateRequest))]
[JsonSerializable(typeof(UserResponse))]
[JsonSerializable(typeof(AuthorizationRequest))]
[JsonSerializable(typeof(AuthorizationResponse))]
public partial class MinimalApiJsonSerializerContext : JsonSerializerContext
{
}
