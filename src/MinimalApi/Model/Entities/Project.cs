using System;

namespace MinimalApi;

public class Project
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string DataPath { get; set; }
    public bool IsPublicRead { get; set; }
    public string Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
