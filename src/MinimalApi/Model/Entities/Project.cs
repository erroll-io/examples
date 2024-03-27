using System;

namespace MinimalApi;

public class Project
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string DataPath { get; set; }
    public string Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}

public class DataType
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Metadata { get; set; }
}

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

public class ProjectData
{
    public string ProjectId { get; set; }
    public string DataRecordId { get; set; }
    public string Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
