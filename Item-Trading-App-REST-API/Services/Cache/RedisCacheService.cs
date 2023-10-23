using StackExchange.Redis;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.Cache;

public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;

    private IDatabase Database
    {
        get => _connectionMultiplexer.GetDatabase();
    }

    public RedisCacheService(IConnectionMultiplexer connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer;
    }

    public async Task<string> GetCacheValueAsync(string key)
    {
        return (await Database.StringGetAsync(key)).ToString();
    }

    public async Task<T> GetCacheValueAsync<T>(string key)
    {
        var value = await GetCacheValueAsync(key);

        if (string.IsNullOrEmpty(value)) return default;

        return JsonSerializer.Deserialize<T>(value);
    }

    public Task SetCacheValueAsync(string key, string value) =>
        Database.StringSetAsync(key, value);

    public Task SetCacheValueAsync<T>(string key, T value) =>
        SetCacheValueAsync(key, JsonSerializer.Serialize(value));

    public Task<bool> ContainsKey(string key) =>
        Database.KeyExistsAsync(key);

    public async Task<Dictionary<string, T>> ListWithPrefix<T>(string prefix, bool removePrefix = false)
    {
        var dictionary = new Dictionary<string, T>();
        var endPoints = _connectionMultiplexer.GetEndPoints();

        foreach (var endpoint in endPoints)
        {
            IServer server = _connectionMultiplexer.GetServer(endpoint);
            var keys = server.KeysAsync(Database.Database, prefix + "*");
            await foreach (var key in keys)
            {
                var value = await GetCacheValueAsync(key);
                dictionary.Add(removePrefix ? key.ToString().Replace(prefix, string.Empty) : key, typeof(T) == typeof(string) ? (T) (value as object) : JsonSerializer.Deserialize<T>(value));
            }
        }

        return dictionary;
    }

    public Task ClearCacheKeyAsync(string key) =>
        Database.KeyDeleteAsync(key);
}
