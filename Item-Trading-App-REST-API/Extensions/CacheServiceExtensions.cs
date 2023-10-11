using Item_Trading_App_REST_API.Services.Cache;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace Item_Trading_App_REST_API.Extensions;

public static class CacheServiceExtensions
{
    public static async Task<List<T>> GetEntitiesAsync<T>(this ICacheService service, string cacheKey, Func<object[], Task<List<T>>> readFromDb, bool setCache, params object[] args) where T : class
    {
        var entities = (await service.ListWithPrefix<T>(cacheKey)).Values.ToList();

        if (entities.Count == 0)
        {
            entities = await readFromDb(args);

            if (setCache)
                foreach (var entity in entities)
                    await service.SetCacheValueAsync(cacheKey + (args[0] as Func<T, string>)(entity), entity);
        }

        return entities;
    }

    public static async Task<List<T>> GetEntitiesAsync<T>(this ICacheService service, string cacheKey, Func<object[], Task<List<T>>> readFromDb, Func<object, Task<T>> convert, bool setCache, params object[] args) where T : class
    {
        var entities = (await service.ListWithPrefix<T>(cacheKey)).Values.ToList();

        if (entities.Count == 0)
        {
            entities = await readFromDb(args);

            if (setCache)
                foreach (var entity in entities)
                {
                    var tmp = await convert(entity);
                    await service.SetCacheValueAsync(cacheKey + (args[0] as Func<T, string>)(tmp), tmp);
                }
        }
        return entities;
    }

    public static async Task<List<string>> GetEntityIdsAsync(this ICacheService service, string cacheKey, Func<object[], Task<List<string>>> readFromDb, bool setCache, params object[] args)
    {
        var ids = (await service.ListWithPrefix<string>(cacheKey, true)).Keys.ToList();

        if (ids.Count == 0)
        {
            var entities = await readFromDb(args);

            if (setCache)
                foreach (var entity in entities)
                {
                    await service.SetCacheValueAsync(cacheKey + entity, "");
                    ids.Add(entity);
                }
        }

        return ids;
    }

    public static async Task<T> GetEntityAsync<T>(this ICacheService service, string cacheKey, Func<object[], Task<T>> readFromDb, bool setCache = false, params object[] args) where T : new()
    {
        var entity = await service.GetCacheValueAsync<T>(cacheKey);

        if (entity is null)
        {
            entity = await readFromDb(args);

            if (setCache && entity is not null)
                await service.SetCacheValueAsync(cacheKey, entity);
        }

        return entity;
    }
}
