using System;

namespace MinimalApi;

public class HealthResponse
{
    public string RequestorIp { get; set; }
    public string Echo { get; set; }
    public DateTime Now { get; set; }
    public DateTime Then { get; set; }
}
