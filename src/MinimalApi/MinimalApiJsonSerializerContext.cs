using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json.Serialization;

using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using MinimalApi.Services;

namespace MinimalApi;

[JsonSerializable(typeof(ILambdaContext))]
[JsonSerializable(typeof(APIGatewayHttpApiV2ProxyRequest))]
[JsonSerializable(typeof(APIGatewayHttpApiV2ProxyResponse))]
[JsonSerializable(typeof(HealthRequest))]
[JsonSerializable(typeof(HealthResponse))]
[JsonSerializable(typeof(UserCreateRequest))]
[JsonSerializable(typeof(UserResponse))]
[JsonSerializable(typeof(ProjectCreateRequest))]
[JsonSerializable(typeof(ProjectResponse))]
[JsonSerializable(typeof(ProjectsResponse))]
[JsonSerializable(typeof(ProjectUserResponse))]
[JsonSerializable(typeof(IEnumerable<ProjectUserResponse>))]
[JsonSerializable(typeof(DataRecordRequest))]
[JsonSerializable(typeof(DataRecordResponse))]
[JsonSerializable(typeof(IEnumerable<DataRecordResponse>))]
[JsonSerializable(typeof(DataResponse))]
[JsonSerializable(typeof(AuthorizationRequest))]
[JsonSerializable(typeof(AuthorizationResponse))]

[JsonSerializable(typeof(Permission))]
[JsonSerializable(typeof(Role))]
[JsonSerializable(typeof(RolePermission))]
[JsonSerializable(typeof(UserRole))]
[JsonSerializable(typeof(User))]

[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(ClaimLite))]
[JsonSerializable(typeof(List<ClaimLite>))]
[JsonSerializable(typeof(CedarSharp.AvpPolicy))]
[JsonSerializable(typeof(List<CedarSharp.AvpPolicy>))]

[JsonSerializable(typeof(AuthorizationComparisonResponse))]
[JsonSerializable(typeof(AuthorizationComparisonResult))]

#if DEBUG
[JsonSerializable(typeof(SeedData))]
[JsonSerializable(typeof(SeedRole))]
#endif
public partial class MinimalApiJsonSerializerContext : JsonSerializerContext
{
}
