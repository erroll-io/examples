using System;
using System.Collections.Generic;

namespace MinimalApi;

public class DataRecordRequest
{
    public string DataTypeId { get; set; }
    public string FileName { get; set; }
    public ulong? Size { get; set; }
    public string Metadata { get; set; }
}

public class DataFinalizeUploadRequest
{
    public string UploadId { get; set; }
    public IEnumerable<string> Parts { get; set; }
}
