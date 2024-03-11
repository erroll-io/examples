using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MinimalApi;

public static class ConfigurationLogic
{
    // TODO: these AddConfig extensions don't work with AOT, presumably due to
    // the source generator being able to intercept e.g. Configure<ActualType>()
    // but _not_ able to intercept e.g. Configure<TConfig>(). Investigate.

    public static IServiceCollection AddConfig<TConfig>(
        this WebApplicationBuilder builder)
            where TConfig : class
    {
        throw new NotSupportedException();

        return builder.Services.AddConfig<TConfig>(builder.Configuration);
    }

    public static IServiceCollection AddConfig<TConfig>(
        this IServiceCollection services,
        IConfiguration configuration)
            where TConfig : class
    {
        throw new NotSupportedException();

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
