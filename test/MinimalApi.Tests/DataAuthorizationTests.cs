using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using MinimalApi.Services;

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
}
