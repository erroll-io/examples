using System.Collections.Generic;

namespace MinimalApi;

public class AvpConfig
{
    public string PolicyStoreId { get; set; }
    public ICollection<PolicyTemplate> PolicyTemplates { get; set; }
}

public class PolicyTemplate
{
    public string TemplateId { get; set; }
    public string RoleName { get; set; }
    public string Statement { get; set; }
}
