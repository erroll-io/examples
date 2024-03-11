using System.Collections.Generic;

namespace MinimalApi;

public class AwsConfig
{
    public string AccountId { get; set; }
}

public class OauthConfig
{
    public IEnumerable<string> RedirectUrls { get; set; }
    public string TestSecret { get; set; }
}
