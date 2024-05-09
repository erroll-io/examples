using System;

namespace MinimalApi;

public class AuthConfig
{
    [Obsolete("For temporary testing purposes only.")]
    public bool DoUseAvp { get; set; } = false;
}
