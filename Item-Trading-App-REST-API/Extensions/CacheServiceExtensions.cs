using Item_Trading_App_REST_API.Services.Cache;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace Item_Trading_App_REST_API.Extensions;

public static class CacheServiceExtensions
{
    public static async Task<T[]> GetEntitiesAsync<T>(this ICacheService service, string cacheKey, Func<object[], Task<T[]>> readFromDb, bool setCache, params object[] args) where T : class
    {
        var entities = (await service.ListWithPrefix<T>(cacheKey))?.Values.ToArray();

        if (entities is null || entities.Length == 0)
        {
            entities = await readFromDb(args);

            if (setCache)
                foreach (var entity in entities)      
                    await service.SetCacheValueAsync($"{cacheKey}{(args[0] as Func<T, string>)(entity)}", entity);
        }

        return entities;
    }

    public static async Task<T[]> GetEntitiesAsync<T, R>(this ICacheService service, string cacheKey, Func<object[], Task<R[]>> readFromDb, Func<R, Task<T>> convert, bool setCache, params object[] args) where T : class
    {
        var entities = (await service.ListWithPrefix<T>(cacheKey))?.Values.ToList() ?? new List<T>();

        if (entities.Count == 0)
        {
            var fromDb = await readFromDb(args);

            if (setCache)
                foreach (var entity in fromDb)
                {
                    var tmp = await convert(entity);
                    await service.SetCacheValueAsync($"{cacheKey}{(args[0] as Func<T, string>)(tmp)}", tmp);
                    entities.Add(tmp);
                }
            else
                foreach (var entity in fromDb)
                {
                    var tmp = await convert(entity);
                    entities.Add(tmp);
                }
        }
        return entities.ToArray();
    }

    public static async Task<string[]> GetSetValuesAsync(this ICacheService service, string cacheKey, Func<object[], Task<string[]>> readFromDb, bool setCache, params object[] args)
    {
        var values = (await service.ListSetValuesAsync(cacheKey)).ToArray();

        if (values is null || values.Length == 0)
        {
            values = await readFromDb(args);

            if (setCache)
                await service.AddToSet(cacheKey, values);
        }

        return values;
    }

    public static async Task<string[]> GetEntityIdsAsync(this ICacheService service, string cacheKey, Func<object[], Task<string[]>> readFromDb, bool setCache, params object[] args)
    {
        var ids = (await service.ListWithPrefix<string>(cacheKey, true)).Keys.ToList();

        if (ids.Count == 0)
        {
            var entities = await readFromDb(args);

            if (setCache)
                foreach (var entity in entities)
                {
                    await service.SetCacheValueAsync($"{cacheKey}{entity}", "");
                    ids.Add(entity);
                }
        }

        return ids.ToArray();
    }

    public static async Task<T> GetEntityAsync<T>(this ICacheService service, string cacheKey, Func<object[], Task<T>> readFromDb, bool setCache = false, params object[] args) where T : new()
    {
        var isNullable = Nullable.GetUnderlyingType(typeof(T)) != null;

        var entity = await service.GetCacheValueAsync<T>(cacheKey);
        
        if ((isNullable && entity is null) || !await service.ContainsKey(cacheKey))
        {
            entity = await readFromDb(args);

            if (setCache && entity is not null)
                await service.SetCacheValueAsync(cacheKey, entity);
        }

        return entity;
    }
}
