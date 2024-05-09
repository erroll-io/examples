using System.Collections.Generic;

namespace MinimalApi;

public class AvpConfig
{
    public string PolicyStoreId { get; set; }
    public Dictionary<string, string> RoleTemplates { get; set; }
}
