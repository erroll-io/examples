namespace MinimalApi;

public class AuthorizationRequest
{
    public string Policy { get; set; }
    public string Principal { get; set; }
    public string Action { get; set; }
    public string Resource { get; set; }
}
