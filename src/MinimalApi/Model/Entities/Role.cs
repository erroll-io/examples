using System;

namespace MinimalApi;

public class Role
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
}

public class Permission
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
}

public class RolePermission
{
    public string RoleId { get; set; }
    public string PermissionId { get; set; }
}

public class UserRole
{
    public string Id { get; set; }
    public string UserId { get; set; }
    public string RoleId { get; set; }
    public string Condition { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
}

public class AuthRecord
{
    public string PK { get; set; }
    public string RK { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
}