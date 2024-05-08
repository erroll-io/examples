namespace MinimalApi.Services;

public class ClaimLite
{
    public string Type { get; set; }
    public string Value { get; set; }

    public ClaimLite()
    {
    }

    public ClaimLite(string type, string value)
    {
        Type = type;
        Value = value;
    }
}
