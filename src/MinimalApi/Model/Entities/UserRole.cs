using System;

namespace MinimalApi;

public class UserRole
{
    public string Id { get; set; }
    public string UserId { get; set; }
    public string RoleId { get; set; }
    public string Condition { get; set; }
    //public string Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
}
