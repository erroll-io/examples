using System;
using System.Linq;
using System.Threading.Tasks;

using Amazon.DynamoDBv2;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MinimalApi.Services;
using NSubstitute;

namespace MinimalApi.Tests;

public class DataAuthorizationTests : IntegrationTestBase
{
    public DataAuthorizationTests()
        : base (testSeedDataFileName: "ProjectAuthorizationTestData.json")
    {
    }

    [Theory]
    [InlineData("user-one", true)]
    [InlineData("user-two", true)]
    [InlineData("user-three", true)]
    [InlineData("", false)]
    public async Task CreateUserDataReturnsExpected(string userSub, bool expectedAuthResult)
    {
        var dataService = ServiceProvider.GetRequiredService<IDataService>();
        var principal = await ServiceProvider.GetRequiredService<ClaimsPrincipalFactory>()
            .GetClaimsPrincipal(userSub);

        var dataResult = await dataService.CreateDataRecord(
            principal,
            new DataRecordParams()
            {
                DataTypeId = "bmp",
                FileName = "peppers.bmp",
                Size = 263168
            });

        Assert.Equal(expectedAuthResult, dataResult.AuthorizationResult.Succeeded);
    }

    [Theory]
    [InlineData("user-one", 1)]
    [InlineData("user-two", 1)]
    [InlineData("user-three", 1)]
    public async Task GetUserDataReturnsExpected(string userSub, int expectedCount)
    {
        var dataService = ServiceProvider.GetRequiredService<IDataService>();
        var principal = await ServiceProvider.GetRequiredService<ClaimsPrincipalFactory>()
            .GetClaimsPrincipal(userSub);

        var dataResult = await dataService.GetUserData(principal);

        Assert.True(dataResult.AuthorizationResult.Succeeded);

        Assert.Equal(expectedCount, dataResult.Result.Count());
    }

    [Theory]
    [InlineData("user-one", "user-one-gvcf", true)]
    [InlineData("user-one", "user-two-gvcf", false)]
    [InlineData("user-one", "user-three-pdf", false)]
    [InlineData("user-two", "user-one-gvcf", false)]
    [InlineData("user-two", "user-two-gvcf", true)]
    [InlineData("user-two", "user-three-pdf", false)]
    [InlineData("user-three", "user-one-gvcf", false)]
    [InlineData("user-three", "user-two-gvcf", false)]
    [InlineData("user-three", "user-three-pdf", true)]
    public async Task GetDataRecordReturnsExpected(string userSub, string dataRecordId, bool expectedAuthResult)
    {
        var dataService = ServiceProvider.GetRequiredService<IDataService>();
        var principal = await ServiceProvider.GetRequiredService<ClaimsPrincipalFactory>()
            .GetClaimsPrincipal(userSub);

        var dataResult = await dataService.GetDataRecord(principal, dataRecordId);

        Assert.Equal(expectedAuthResult, dataResult.AuthorizationResult.Succeeded);
    }

    [Fact]
    public async Task DataOwnerRoleIsAssignedUponDataCreation()
    {
        var principal = await ServiceProvider.GetRequiredService<ClaimsPrincipalFactory>()
            .GetClaimsPrincipal("user-one");

        var mockUserRoleService = Substitute.For<IUserRoleService>();
        mockUserRoleService
            .CreateUserRole(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>())
            .Returns(
                new UserRole()
                {
                });

        var dataService = new DataService(
            ServiceProvider.GetRequiredService<IAuthorizationService>(),
            ServiceProvider.GetRequiredService<IAmazonDynamoDB>(),
            mockUserRoleService,
            ServiceProvider.GetRequiredService<IUserService>(),
            ServiceProvider.GetRequiredService<IOptions<DynamoConfig>>());

        var dataRecordResult = await dataService.CreateDataRecord(
            principal,
            new DataRecordParams
            {
                DataTypeId = "pdf",
                FileName = "foo.pdf",
                Size = 42
            });

        Assert.True(dataRecordResult.IsSuccess);

        await mockUserRoleService.Received().CreateUserRole(
            Arg.Any<string>(),
            "MinimalApi::Role::DataOwner",
            $"DataRecord::{dataRecordResult.Result.Id}");
    }
}
