using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

using MinimalApi.Services;

namespace MinimalApi;

public class DynamoSeeder
{
    internal static readonly string _seedDataPath = Path.Combine(
        Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location),
        "Resources",
        "SeedData.json");

    private readonly IProjectService _projectService;
    private readonly IRoleService _roleService;
    private readonly IPermissionService _permissionService;
    private readonly IUserRoleService _userRoleService;
    private readonly IUserService _usersService;

    public DynamoSeeder(
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

    public async Task SeedData()
    {
        var jsonSerializerOptions = new JsonSerializerOptions();
        jsonSerializerOptions.TypeInfoResolverChain.Add(MinimalApiJsonSerializerContext.Default);
        jsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        jsonSerializerOptions.PropertyNameCaseInsensitive = true;

        var seedData = JsonSerializer.Deserialize<SeedData>(
            await File.ReadAllTextAsync(_seedDataPath),
            jsonSerializerOptions);
        
        if (seedData == null)
        {
            throw new Exception("Failed to deserialize seed data.");
        }

        await SeedProjects(seedData);
        //await SeedProjectUsers(seedData);
        await SeedPermissions(seedData);
        await SeedRoles(seedData);
        await SeedUsers(seedData);
        await SeedUserRoles(seedData);
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

    //private async Task SeedProjectUsers(SeedData seedData)
    //{
    //    foreach (var seedProjectUser in seedData.ProjectUsers)
    //    {
    //        var projectUser = await _projectService.GetProjectUser(seedProjectUser.Id);

    //        if (projectUser == default)
    //        {
    //            projectUser = new ProjectUser()
    //            {
    //                Id = seedProjectUser.Id,
    //                ProjectId = seedProjectUser.ProjectId,
    //                UserId = seedProjectUser.UserId,
    //                CreatedAt = DateTime.UtcNow
    //            };

    //            await _projectService.SaveProjectUser(projectUser);
    //        }
    //    }
    //}


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
                role = await _roleService.SaveRole(
                    new Role()
                    {
                        Id = seedRole.Id,
                        Name = seedRole.Name,
                        Description = seedRole.Description
                    }
                );

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
                userRole = await _userRoleService.SaveUserRole(
                    new UserRole()
                    {
                        Id = seedUserRole.Id,
                        UserId = seedUserRole.UserId,
                        RoleId = seedUserRole.RoleId,
                        Condition = seedUserRole.Condition,
                        CreatedAt = DateTime.UtcNow
                    }
                );

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

public class SeedData
{
    public IEnumerable<Project> Projects { get; set; }
    //public IEnumerable<ProjectUser> ProjectUsers { get; set; }
    public IEnumerable<DataType> DataTypes { get; set; }
    public IEnumerable<DataRecord> DataRecords { get; set; }
    //public IEnumerable<UserData> UserData { get; set; }
    public IEnumerable<ProjectData> ProjectData { get; set; }
    public IEnumerable<Permission> Permissions { get; set; }
    public IEnumerable<SeedRole> Roles { get; set; }
    public IEnumerable<User> Users { get; set; }
    public IEnumerable<UserRole> UserRoles { get; set; }
}

public class SeedRole : Role
{
    public IEnumerable<RolePermission> RolePermissions { get; set; }
}
