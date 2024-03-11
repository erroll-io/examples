namespace MinimalApi.Services;

public class UserCreateParams
{
    public string PrincipalId { get; set; }
    public string Email { get; set; }
    public string Language { get; set; }
    public string Timezone { get; set; }
    public string Metadata { get; set; }
}
