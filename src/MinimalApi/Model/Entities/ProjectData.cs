using System;

namespace MinimalApi;

public class ProjectData
{
    public string ProjectId { get; set; }
    public string DataRecordId { get; set; }
    public string Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
