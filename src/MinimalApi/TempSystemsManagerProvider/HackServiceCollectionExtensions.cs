using System;

using Amazon.Extensions.Configuration.SystemsManager;
using Amazon.Extensions.Configuration.SystemsManager.Internal;
using Amazon.Extensions.NETCore.Setup;

namespace Microsoft.Extensions.Configuration;

public static class ConfigurationBuilderExtensions
{
    public static IConfigurationBuilder AddSystemsManagerWithHack(
        this IConfigurationBuilder builder,
        string path)
    {
        var source = new HackSystemsManagerConfigurationSource();
        source.Path = path;

        if (string.IsNullOrWhiteSpace(source.Path)) throw new ArgumentNullException(nameof(source.Path));
        if (source.AwsOptions != null) return builder.Add(source);

        source.AwsOptions = AwsOptionsProvider.GetAwsOptions(builder);
        return builder.Add(source);
    }
}
