using System;

using Microsoft.AspNetCore.Authorization;

namespace MinimalApi;

public class OperationRequirement : IAuthorizationRequirement
{
    public string Operation { get; set; }
    public string Condition { get; set; }

    [Obsolete]
    public string Strategy { get; set; }

    public OperationRequirement()
    {
    }

    public OperationRequirement(string operation)
    {
        Operation = operation;
    }

    public OperationRequirement(string operation, string condition)
        : this(operation)
    {
        Condition = condition;
    }

    [Obsolete]
    public OperationRequirement(string operation, string condition, string strategy)
        : this(operation, condition)
    {
        Strategy = strategy;
    }
}
