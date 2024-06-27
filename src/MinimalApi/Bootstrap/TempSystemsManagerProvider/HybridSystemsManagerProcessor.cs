using System;
using System.Collections.Generic;
using System.Linq;

using Amazon.SimpleSystemsManagement.Model;

namespace Amazon.Extensions.Configuration.SystemsManager.Internal
{
    // The [current implementation](https://github.com/aws/aws-dotnet-extensions-configuration/blob/aa02b6ee4e4139d6a4f84b1b0742291c8c786c33/src/Amazon.Extensions.Configuration.SystemsManager/Internal/SystemsManagerProcessor.cs#L38-L41) supports JSON values from SecretsManager, but not from SSM. Here we override the default processor and use `JsonConfigurationParser` for any SSM param values starting with the `"JSON_ENCODED"` sentinel. 
    public class HybridParameterProcessor : DefaultParameterProcessor
    {
        private const string _jsonSentinel = "JSON_ENCODED";

        public override IDictionary<string, string> ProcessParameters(IEnumerable<Parameter> parameters, string path)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var parameter in parameters.Where(parameter => IncludeParameter(parameter, path)))
            {
                if (parameter.Value.StartsWith(_jsonSentinel))
                {
                    var parameterDictionary = JsonConfigurationParser
                        .Parse(parameter.Value.Substring(_jsonSentinel.Length));

                    foreach (var keyValue in parameterDictionary)
                    {
                        result[$"{GetKey(parameter, path)}:{keyValue.Key}"] = keyValue.Value;
                    }
                }
                else
                {
                    result[GetKey(parameter, path)] = GetValue(parameter, path);
                }
            }

            return result;
        }
    }
}
