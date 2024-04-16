    using Microsoft.AspNetCore.Authorization;

    namespace MinimalApi;

    public class OperationRequirement : IAuthorizationRequirement
    {
        public string Operation { get; set; }
        public string Condition { get; set; }

        public OperationRequirement()
        {
        }

        public OperationRequirement(string operation, string condition)
        {
            Operation = operation;
            Condition = condition;
        }
    }