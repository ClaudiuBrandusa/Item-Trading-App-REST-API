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

    public Task<long> CountSetMembers(string key)
    {
        return Database.SetLengthAsync(key);
    }

    public Task SetCacheValueAsync(string key, string value) =>
        Database.StringSetAsync(key, value);

    public Task SetCacheValueAsync<T>(string key, T value) =>
        SetCacheValueAsync(key, JsonSerializer.Serialize(value));

    public Task AddToSet(string key, string value)
    {
        return Database.SetAddAsync(key, value);
    }

    public async Task AddToSet(string key, string[] values)
    {
        for(int i = 0; i < values.Length; i++)
        {
            await AddToSet(key, values[i]);
        }
    }

    public Task<bool> ContainsKey(string key) =>
        Database.KeyExistsAsync(key);

    public async Task<bool> AnyKey(string prefix)
    {
        var endPoints = _connectionMultiplexer.GetEndPoints();

        for(int i = 0; i< endPoints.Length; i++)
        {
            var server = _connectionMultiplexer.GetServer(endPoints[i]);

            var keys = server.KeysAsync(Database.Database, $"{prefix}*");

            await foreach (var key in keys)
            {
                return true;
            }
        }

        return false;
    }

    public Task<bool> SetContainsValue(string key, string value)
    {
        return Database.SetContainsAsync(key, value);
    }

    public async Task<Dictionary<string, T>> ListWithPrefix<T>(string prefix, bool removePrefix = false)
    {
        var dictionary = new Dictionary<string, T>();
        var endPoints = _connectionMultiplexer.GetEndPoints();

        foreach (var endpoint in endPoints)
        {
            IServer server = _connectionMultiplexer.GetServer(endpoint);
            var keys = server.KeysAsync(Database.Database, $"{prefix}*");
            await foreach (var key in keys)
            {
                var value = await GetCacheValueAsync(key);
                dictionary.Add(removePrefix ? key.ToString().Replace(prefix, string.Empty) : key, typeof(T) == typeof(string) ? (T) (value as object) : JsonSerializer.Deserialize<T>(value));
            }
        }

        return dictionary;
    }

    public async Task<string[]> ListSetValuesAsync(string key)
    {
        var members = await Database.SetMembersAsync(key);

        return members.ToStringArray();
    }

    public Task ClearCacheKeyAsync(string key) =>
        Database.KeyDeleteAsync(key);

    public async Task ClearCacheKeysStartingWith(string key)
    {
        var endPoints = _connectionMultiplexer.GetEndPoints();

        for(int i = 0; i < endPoints.Length; i++)
        {
            IServer server = _connectionMultiplexer.GetServer(endPoints[i]);
            var cacheKeys = server.KeysAsync(Database.Database, $"{key}");
            await foreach (var cacheKey in cacheKeys)
            {
                await Database.KeyDeleteAsync(cacheKey);
            }
        }
    }

    public async Task RemoveFromSet(string key, string value)
    {
        await Database.SetRemoveAsync(key, value);

        long length = await CountSetMembers(key);

        if (length == 0)
            await ClearCacheKeyAsync(key);
    }
}
