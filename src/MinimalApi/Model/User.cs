using System;

namespace MinimalApi;

public class User
{
    public string Id { get; set; }
    public string PrincipalId { get; set; }
    public string EmailHash { get; set; }
    public string Name { get; set; }
    public string Timezone { get; set; }
    public string Language { get; set; }
    public string Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
