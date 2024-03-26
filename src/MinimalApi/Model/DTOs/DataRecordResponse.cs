using System;

namespace MinimalApi;

public class DataRecordResponse
{
    public string Id { get; set; }
    public string DataTypeId { get; set; }
    public string FileName { get; set; }
    public string Location { get; set; }
    public ulong Size { get; set; }
    public string CreatedBy { get; set; }
    public string Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
}
