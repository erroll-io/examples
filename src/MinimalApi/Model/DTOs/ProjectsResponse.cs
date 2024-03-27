using System.Collections.Generic;

namespace MinimalApi;

public class ProjectsResponse
{
    public IEnumerable<ProjectResponse> Projects { get; set; }
}
