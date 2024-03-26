namespace MinimalApi.Services;

public class DataRecordParams
{
    public string DataTypeId { get; set; }
    public string FileName { get; set; }
    public ulong Size { get; set; }
    public string Metadata { get; set; }
}
