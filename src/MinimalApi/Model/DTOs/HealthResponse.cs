using System;
using System.Collections.Generic;

namespace MinimalApi;

public class HealthResponse
{
    public string RequestorIp { get; set; }
    public string Echo { get; set; }
    public DateTime Now { get; set; }
    public DateTime Then { get; set; }

    public string AwsAccountId { get; set; }
    public IEnumerable<string> AllowedOrigins { get; set; }
    public string TestSecret { get; set; }
}
