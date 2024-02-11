using StackExchange.Redis;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.Cache;

public interface ICacheService
{
    Task<string> GetCacheValueAsync(string key);

    Task<T> GetCacheValueAsync<T>(string key);

    Task<long> CountSetMembers(string key);

    Task SetCacheValueAsync(string key, string value);

    Task SetCacheValueAsync<T>(string key, T value);

    Task AddToSet(string key, string value);

    Task AddToSet(string key, string[] values);

    Task<bool> ContainsKey(string key);

    Task<bool> AnyKey(string prefix);

    Task<bool> SetContainsValue(string key, string value);
    
    Task<Dictionary<string, T>> ListWithPrefix<T>(string prefix, bool removePrefix = false);

    Task<string[]> ListSetValuesAsync(string key);

    Task ClearCacheKeyAsync(string key);

    Task ClearCacheKeysStartingWith(string key);

    Task RemoveFromSet(string key, string value);

    Task RemoveFromSet(string key, RedisValue[] values);
}
