using System;

namespace MinimalApi;

public class HealthRequest
{
    public string Echo { get; set; }
    public DateTime Now { get; set; }
}
