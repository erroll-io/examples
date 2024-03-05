using System;
using Microsoft.Extensions.Configuration;

namespace MinimalApi;

public static class ConfigurationLogic
{
    public static IConfigurationSection GetSection<TConfig>(
        this IConfiguration configuration)
            where TConfig : class
    {
        var sectionName = typeof(TConfig).Name;
        var section = configuration
            .GetSection(sectionName.FromPascalToCamel()) ?? configuration.GetSection(sectionName);

        if (section == null)
        {
            throw new Exception();
        }

        return section;
    }

    private static string FromPascalToCamel(this string value)
    {
        return $"{value[0].ToString().ToLower()}{value.Substring(1)}";
    }
}
