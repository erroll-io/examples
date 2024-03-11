namespace MinimalApi;

public class DynamoConfig
{
    public string UsersTableName { get; set; }
    public string UsersTablePrincipalIdIndexName { get; set; }
    public string UsersTableEmailHashIndexName { get; set; }
}
