using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.Extensions.Caching.Distributed;

namespace MinimalApi.Services;

public static class CacheLogic
{
    private static readonly JsonSerializerOptions _serializerOptions;

    static CacheLogic()
    {
        _serializerOptions = new JsonSerializerOptions();
        _serializerOptions.TypeInfoResolverChain.Add(MinimalApiJsonSerializerContext.Default);
        _serializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        _serializerOptions.PropertyNameCaseInsensitive = true;
    }

    public static async Task<T> Get<T>(this IDistributedCache cache, string key)
    {
        var value = await cache.GetAsync(key);

        if (value == default)
            return default;

        var stringValue = Encoding.UTF8.GetString(value);

        return JsonSerializer.Deserialize<T>(stringValue, _serializerOptions);
    }

    public static Task Set(
        this IDistributedCache cache,
        string key,
        object value,
        TimeSpan ttl = default)
    {
        var stringValue = JsonSerializer.Serialize(value, _serializerOptions);

        // TODO: perhaps set this based on JWT exp
        if (ttl == default)
            ttl = TimeSpan.FromMinutes(5);

        return cache.SetAsync(
            key,
            Encoding.UTF8.GetBytes(stringValue),
            new DistributedCacheEntryOptions()
            {
                AbsoluteExpiration = DateTime.UtcNow + ttl
            });
    }
}
