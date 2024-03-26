using System.Collections.Generic;

namespace MinimalApi;

public class DataResponse
{
    public IEnumerable<DataRecordResponse> Data { get; set; }
}
