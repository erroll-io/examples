using System;

namespace MinimalApi;

public class DataRecord
{
    public string Id { get; set; }
    public string DataTypeId { get; set; }
    public string FileName { get; set; }
    public string Location { get; set; }
    public ulong Size { get; set; }
    public string Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    public DateTime? ValidatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
