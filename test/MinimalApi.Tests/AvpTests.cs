using System;
using System.Linq;
using System.Threading.Tasks;

using Amazon.VerifiedPermissions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using MinimalApi.Services;
using Xunit;

namespace MinimalApi.Tests;

public class AvpTests
{
    private static readonly Lazy<IServiceProvider> _serviceProviderLazy =
        new Lazy<IServiceProvider>(
            () =>
            {
                var builder = WebApplication.CreateSlimBuilder();
                builder.ConfigureApplicationConfiguration("/minimal-api");
                builder.ConfigureApplicationServices();
                builder.Services.AddTransient<AvpUserRoleService>(provider =>
                    new AvpUserRoleService(
                        provider.GetRequiredService<IAmazonVerifiedPermissions>(),
                        () => AvpValueCache.Initialize(
                            provider.GetRequiredService<IAmazonVerifiedPermissions>(),
                            provider.GetRequiredService<IOptions<AvpConfig>>()),
                        provider.GetRequiredService<IOptions<AvpConfig>>()));

                return builder.Build().Services;
            },
            true);
    private static IServiceProvider _serviceProvider => _serviceProviderLazy.Value;

    public AvpTests()
    {
    }

    [Fact]
    public async Task CanAssign_List_AndRemoveRole()
    {
        var principalId = "42424242-4242-4242-4242-424242424242";
        var roleId = "MinimalApi::Role::ProjectAdmin";
        var condition = "MinimalApi::Project:42424242-4242-4242-4242-424242424242";

        var service = _serviceProvider.GetRequiredService<IUserRoleService>();

        var policyId = string.Empty;

        try
        {
            var newUserRole = await service.CreateUserRole(principalId, roleId, condition);
            policyId = newUserRole.Id;

            var userRoles = (await service.GetUserRolesByUserId(principalId)).ToArray();

            Assert.Equal(1, userRoles.Length);
            Assert.Equal(newUserRole.RoleId, userRoles[0].RoleId);

            var projectAdmins = (await service.GetUserRolesByRoleCondition(
                roleId,
                condition)).ToArray();

            Assert.Equal(1, projectAdmins.Length);
            Assert.Equal(principalId, projectAdmins[0].UserId);
        }
        catch (Exception e)
        {
            Console.WriteLine($"ERROR: {e}");
        }
        finally
        {
            if (service != default)
            {
                await service.DeleteUserRole(principalId, roleId, condition);
            }
        }
    }
}
