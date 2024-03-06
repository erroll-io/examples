using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MinimalApi;

public static class ConfigurationLogic
{
    public static IServiceCollection AddConfig<TConfig>(
        this WebApplicationBuilder builder)
            where TConfig : class
    {
        return builder.Services.AddConfig<TConfig>(builder.Configuration);
    }

    public static IServiceCollection AddConfig<TConfig>(
        this IServiceCollection services,
        IConfiguration configuration)
            where TConfig : class
    {
        return services.Configure<TConfig>(
            configuration.GetSection<TConfig>());
    }

    public static IConfigurationSection GetSection<TConfig>(
        this IConfiguration configuration)
            where TConfig : class
    {
        var sectionName = typeof(TConfig).Name;

        var section = configuration.GetSection(sectionName.FromPascalToCamel())
            ?? configuration.GetSection(sectionName);

        if (section == null)
        {
            throw new Exception($"No config section found for {sectionName}.");
        }

        return section;
    }

    private static string FromPascalToCamel(this string value)
    {
        return $"{value[0].ToString().ToLower()}{value.Substring(1)}";
    }
}
