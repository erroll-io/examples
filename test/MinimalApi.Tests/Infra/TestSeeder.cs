using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

using MinimalApi.Services;

namespace MinimalApi.Tests;

public class TestSeeder
{
    internal static readonly string _appSeedDataPath = Path.Combine(
        Directory.GetCurrentDirectory(),
        //Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location),
        //"../../../src/MinimalApi",
        "Resources",
        "SeedData.json");
    internal static readonly string _testSeedDataPath = Path.Combine(
        Directory.GetCurrentDirectory(),
        //Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location),
        "Resources");

    private readonly IProjectService _projectService;
    private readonly IRoleService _roleService;
    private readonly IPermissionService _permissionService;
    private readonly IUserRoleService _userRoleService;
    private readonly IUserService _usersService;

    public TestSeeder(
        IProjectService projectService,
        IRoleService roleService,
        IPermissionService permissionService,
        IUserRoleService userRoleService,
        IUserService usersService)
    {
        _projectService = projectService;
        _roleService = roleService;
        _permissionService = permissionService;
        _userRoleService = userRoleService;
        _usersService = usersService;
    }

    public async Task SeedData(string seedFileName = default)
    {
        var jsonSerializerOptions = new JsonSerializerOptions();
        jsonSerializerOptions.TypeInfoResolverChain.Add(MinimalApiJsonSerializerContext.Default);
        jsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        jsonSerializerOptions.PropertyNameCaseInsensitive = true;

        var appSeedData = JsonSerializer.Deserialize<SeedData>(
            await File.ReadAllTextAsync(_appSeedDataPath),
            jsonSerializerOptions);
        
        if (appSeedData == null)
        {
            throw new Exception("Failed to deserialize seed data.");
        }

        await SeedRoles(appSeedData);
        await SeedPermissions(appSeedData);

        if (string.IsNullOrEmpty(seedFileName))
            return;

        var testSeedData = JsonSerializer.Deserialize<SeedData>(
            await File.ReadAllTextAsync(Path.Combine(_testSeedDataPath, seedFileName)),
            jsonSerializerOptions);
        
        if (appSeedData == null)
        {
            throw new Exception("Failed to deserialize seed data.");
        }

        await SeedProjects(testSeedData);
        await SeedUsers(testSeedData);
        await SeedUserRoles(testSeedData);
    }

    private async Task SeedProjects(SeedData seedData)
    {
        foreach (var seedProject in seedData.Projects)
        {
            var doSave = false;

            var project = await (_projectService as ProjectService).GetProject(seedProject.Id);

            if (project == default)
            {
                project = new Project()
                {
                    Id = seedProject.Id,
                    Name = seedProject.Name,
                    DataPath = seedProject.DataPath,
                    Description = seedProject.Description,
                    CreatedAt = DateTime.UtcNow
                };

                doSave = true;
            }
            else
            {
                if (project.Name != seedProject.Name)
                {
                    project.Name = seedProject.Name;
                    doSave = true;
                }
                if (project.Description != seedProject.Description)
                {
                    project.Description = seedProject.Description;
                    doSave = true;
                }
            }

            if (doSave)
            {
                await _projectService.SaveProject(project);
            }
        }
    }

    private async Task SeedPermissions(SeedData seedData)
    {
        foreach (var seedPermission in seedData.Permissions)
        {
            var doSave = false;

            var permission = await _permissionService.GetPermission(seedPermission.Id);

            if (permission == default)
            {
                permission = new Permission()
                {
                    Id = seedPermission.Id,
                    Name = seedPermission.Name,
                    Description = seedPermission.Description
                };

                doSave = true;
            }
            else
            {
                if (permission.Name != seedPermission.Name)
                {
                    permission.Name = seedPermission.Name;
                    doSave = true;
                }
                if (permission.Description != seedPermission.Description)
                {
                    permission.Description = seedPermission.Description;
                    doSave = true;
                }
            }

            if (doSave)
            {
                await _permissionService.SavePermission(permission);
            }
        }
    }

    private async Task SeedRoles(SeedData seedData)
    {
        foreach (var seedRole in seedData.Roles)
        {
            var doSave = false;

            var role = await _roleService.GetRole(seedRole.Id);

            if (role == default)
            {
                role = new Role()
                    {
                        Id = seedRole.Id,
                        Name = seedRole.Name,
                        Description = seedRole.Description
                    };

                doSave = true;
            }
            else
            {
                if (role.Name != seedRole.Name)
                {
                    role.Name = seedRole.Name;
                    doSave = true;
                }
                if (role.Description != seedRole.Description)
                {
                    role.Description = seedRole.Description;
                    doSave = true;
                }
            }

            if (doSave)
            {
                await _roleService.SaveRole(role);
            }

            foreach (var seedRolePermission in seedRole.RolePermissions)
            {
                var rolePermission = await _roleService.GetRolePermission(role.Id, seedRolePermission.PermissionId);

                if (rolePermission == default)
                {
                    await _roleService.SaveRolePermission(
                        new RolePermission()
                        {
                            RoleId = role.Id,
                            PermissionId = seedRolePermission.PermissionId
                        });
                }
            }
        }
    }

    private async Task SeedUsers(SeedData seedData)
    {
        foreach (var seedUser in seedData.Users)
        {
            var doSave = false;

            var user = await _usersService.GetUser(seedUser.Id);

            if (user != default)
                continue;

            user = new User()
            {
                Id = seedUser.Id,
                PrincipalId = seedUser.PrincipalId,
            };

            await _usersService.SaveUser(user);
        }
    }

    private async Task SeedUserRoles(SeedData seedData)
    {
        foreach (var seedUserRole in seedData.UserRoles)
        {
            var doSave = false;

            var userRole = await _userRoleService.GetUserRole(seedUserRole.Id);

            if (userRole == default)
            {
                userRole = new UserRole()
                {
                    Id = seedUserRole.Id,
                    UserId = seedUserRole.UserId,
                    RoleId = seedUserRole.RoleId,
                    Condition = seedUserRole.Condition,
                    CreatedAt = DateTime.UtcNow
                };

                doSave = true;
            }
            else
            {
                if (userRole.Condition != seedUserRole.Condition)
                {
                    userRole.Condition = seedUserRole.Condition;
                    doSave = true;
                }
            }

            if (doSave)
            {
                await _userRoleService.SaveUserRole(userRole);
            }
        }
    }
}
