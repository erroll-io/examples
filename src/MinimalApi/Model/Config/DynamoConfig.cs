namespace MinimalApi;

public class DynamoConfig
{
    public string UsersTableName { get; set; }
    public string UsersTablePrincipalIdIndexName { get; set; }
    public string UsersTableEmailHashIndexName { get; set; }
    public string ProjectsTableName { get; set; }
    public string ProjectUsersTableName { get; set; }
    public string DataTableName { get; set; }
    public string UserDataTableName { get; set; }
    public string ProjectDataTableName { get; set; }
    public string PermissionsTableName { get; set; }
    public string RolesTableName { get; set; }
    public string RolePermissionsTableName { get; set; }
    //public string PermissionsTableRoleIndexName { get; set; }
    public string UserRolesTableName { get; set; }
    public string UserRolesTableUserIdIndexName { get; set; }
}
