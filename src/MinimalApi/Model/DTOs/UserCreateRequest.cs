namespace MinimalApi;

public class UserCreateRequest
{
    public string Email { get; set; }
    public string Language { get; set; }
    public string Timezone { get; set; }
    public string Metadata { get; set; }
}
