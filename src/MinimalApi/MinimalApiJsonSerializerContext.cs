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

[JsonSerializable(typeof(Permission))]
[JsonSerializable(typeof(Role))]
[JsonSerializable(typeof(RolePermission))]
[JsonSerializable(typeof(UserRole))]
[JsonSerializable(typeof(User))]

#if DEBUG
[JsonSerializable(typeof(SeedData))]
[JsonSerializable(typeof(SeedRole))]
#endif
public partial class MinimalApiJsonSerializerContext : JsonSerializerContext
{
}
