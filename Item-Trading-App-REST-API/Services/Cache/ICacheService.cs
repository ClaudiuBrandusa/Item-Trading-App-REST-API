using System.Collections.Generic;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.Cache;

public interface ICacheService
{
    Task<string> GetCacheValueAsync(string key);

    Task<T> GetCacheValueAsync<T>(string key);

    Task SetCacheValueAsync(string key, string value);

    Task SetCacheValueAsync<T>(string key, T value);

    Task<bool> ContainsKey(string key);

    Task<Dictionary<string, T>> ListWithPrefix<T>(string prefix, bool removePrefix = false);

    Task ClearCacheKeyAsync(string key);
}
