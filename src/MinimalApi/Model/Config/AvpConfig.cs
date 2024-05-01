using System.Collections.Generic;

namespace MinimalApi;

public class AvpConfig
{
    public string PolicyStoreId { get; set; }
    public Dictionary<string, string> RoleTemplates { get; set; }
    //public List<RoleTemplate> RoleTemplates { get; set; }
}

public class RoleTemplate
{
    public string RoleId { get; set; }
    public string TemplateId { get; set; }
}
