using System;

namespace MinimalApi;

public class UserResponse
{
    public string Id { get; set; }
    public string Timezone { get; set; }
    public string Language { get; set; }
    public string Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
}
