using Application.Services.Cache;

namespace Application.Extensions;

public static class CacheServiceExtensions
{
    public static async Task<CachedType[]> GetEntitiesAsync<CachedType>(this ICacheService service, string cacheKey, Func<object[], Task<CachedType[]>> readFromDb, bool setCache, params object[] args) where CachedType : class
    {
        var entities = (await service.ListWithPrefix<CachedType>(cacheKey))?.Values.ToArray();

        if (entities is null || entities.Length == 0)
        {
            entities = await readFromDb(args);

            if (setCache)
            {
                var func = args[0] as Func<CachedType, string>;

                if (func is not null)
                    foreach (var entity in entities)
                        await service.SetCacheValueAsync($"{cacheKey}{func(entity)}", entity);
            }
        }

        return entities;
    }

    public static async Task<EntityType[]> GetEntitiesAsync<EntityType, CachedType>(this ICacheService service, string cacheKey, Func<object[], Task<EntityType[]>> readFromDb, Func<EntityType, CachedType> convertEntityToCachedEntity, Func<CachedType, EntityType> convertCachedEntityToEntity, bool setCache, params object[] args) where EntityType : class
    {
        var cachedEntities = (await service.ListWithPrefix<CachedType>(cacheKey))?.Values.ToList() ?? new List<CachedType>();

        var entities = new List<EntityType>();

        if (cachedEntities.Count == 0)
        {
            var fromDb = await readFromDb(args);

            if (setCache)
            {
                var func = args[0] as Func<EntityType, string>;

                if (func is not null)
                    foreach (var entity in fromDb)
                    {
                        var tmp = convertEntityToCachedEntity(entity);
                        await service.SetCacheValueAsync($"{cacheKey}{func(entity)}", tmp);
                        entities.Add(entity);
                    }
            }
            else
                foreach (var entity in fromDb)
                {
                    var tmp = convertEntityToCachedEntity(entity);
                    entities.Add(entity);
                }
        }
        else
        {
            entities.AddRange(cachedEntities.Select(x => convertCachedEntityToEntity(x)));
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
                    await service.SetCacheValueAsync($"{cacheKey}{entity}", string.Empty);
                    ids.Add(entity);
                }
        }

        return ids.ToArray();
    }

    public static async Task<CachedType?> GetEntityReferenceAsync<CachedType>(this ICacheService service, string cacheKey, Func<object[], Task<CachedType>> readFromDb, bool setCache = false, params object[] args) where CachedType : class
    {
        var isNullable = !typeof(CachedType).IsValueType || Nullable.GetUnderlyingType(typeof(CachedType)) != null;

        var entity = await service.GetCacheValueAsync<CachedType>(cacheKey);

        if (isNullable && entity is null || !await service.ContainsKey(cacheKey))
        {
            entity = await readFromDb(args);

            if (setCache && entity is not null)
                await service.SetCacheValueAsync(cacheKey, entity);
        }

        return entity;
    }

    public static async Task<EntityType?> GetEntityReferenceAsync<CachedType, EntityType>(this ICacheService service, string cacheKey, Func<object[], Task<EntityType>> readFromDb, Func<EntityType, CachedType> convertEntityToCachedEntity, Func<CachedType, EntityType> convertCachedEntityToEntity, bool setCache = false, params object[] args) where EntityType : class
    {
        var isNullable = !typeof(EntityType).IsValueType || Nullable.GetUnderlyingType(typeof(EntityType)) != null;

        var cached = await service.GetCacheValueAsync<CachedType>(cacheKey);

        EntityType entity = null;

        if (isNullable && cached is null || !await service.ContainsKey(cacheKey))
        {
            entity = await readFromDb(args);

            if (setCache && entity is not null)
                await service.SetCacheValueAsync(cacheKey, convertEntityToCachedEntity(entity));
        }
        else
        {
            entity = convertCachedEntityToEntity(cached);
        }

        return entity;
    }

    public static async Task<CachedType> GetEntityValueAsync<CachedType>(this ICacheService service, string cacheKey, Func<object[], Task<CachedType>> readFromDb, bool setCache = false, params object[] args) where CachedType : struct
    {
        var entity = await service.GetCacheValueAsync<CachedType>(cacheKey);

        if (!await service.ContainsKey(cacheKey))
        {
            entity = await readFromDb(args);

            if (setCache)
                await service.SetCacheValueAsync(cacheKey, entity);
        }

        return entity;
    }
}
