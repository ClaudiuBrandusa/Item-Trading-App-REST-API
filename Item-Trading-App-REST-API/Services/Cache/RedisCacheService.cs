using Newtonsoft.Json;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.Cache;

public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;

    private IDatabase _database
    {
        get => _connectionMultiplexer.GetDatabase();
    }

    public RedisCacheService(IConnectionMultiplexer connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer;
    }

    public async Task<string> GetCacheValueAsync(string key)
    {
        return (await _database.StringGetAsync(key)).ToString();
    }

    public async Task<T> GetCacheValueAsync<T>(string key)
    {
        var value = await GetCacheValueAsync(key);

        if (string.IsNullOrEmpty(value)) return default;

        return JsonConvert.DeserializeObject<T>(value);
    }

    public async Task SetCacheValueAsync(string key, string value)
    {
        await _database.StringSetAsync(key, value);
    }

    public async Task SetCacheValueAsync<T>(string key, T value)
    {
        await SetCacheValueAsync(key, JsonConvert.SerializeObject(value));
    }

    public async Task<bool> ContainsKey(string key)
    {
        return await _database.KeyExistsAsync(key);
    }

    public async Task<Dictionary<string, T>> ListWithPrefix<T>(string prefix, bool removePrefix = false)
    {
        var dictionary = new Dictionary<string, T>();
        var endPoints = _connectionMultiplexer.GetEndPoints();

        foreach (var endpoint in endPoints)
        {
            IServer server = _connectionMultiplexer.GetServer(endpoint);
            var keys = server.KeysAsync(_database.Database, prefix + "*");
            await foreach (var key in keys)
            {
                var value = await GetCacheValueAsync(key);
                dictionary.Add(removePrefix ? key.ToString().Replace(prefix, string.Empty) : key, typeof(T) == typeof(string) ? (T) (value as object) : JsonConvert.DeserializeObject<T>(value));
            }
        }

        return dictionary;
    }

    public async Task ClearCacheKeyAsync(string key)
    {
        await _database.KeyDeleteAsync(key);
    }
}
