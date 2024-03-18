using System.Collections.Generic;

namespace MinimalApi;

public class OAuthConfig
{
    public string AuthorityUrl { get; set; }
    public IEnumerable<string> AllowedOrigins { get; set; }
}
