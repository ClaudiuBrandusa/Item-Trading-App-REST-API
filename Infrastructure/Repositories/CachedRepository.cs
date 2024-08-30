using Application.Services.Cache;
using Application.Repositories;
using Domain.Repositories;

namespace Infrastructure.Repositories;

public abstract class CachedRepository : ICachedRepository
{
    private readonly IRepository _repository;
    protected readonly ICacheService _cacheService;

    protected CachedRepository(IRepository repository, ICacheService cacheService)
    {
        _repository = repository;
        _cacheService = cacheService;
    }

    public async Task<bool> AddEntityAsync<T>(T entity) where T : class
    {
        var result = await _repository.AddEntityAsync(entity);
        
        if (!result) return false;

        await SetCacheAsync(entity);

        return true;
    }

    public async Task<bool> UpdateEntityAsync<T>(T entity) where T : class
    {
        var result = await _repository.UpdateEntityAsync(entity);

        if (!result) return false;

        await SetCacheAsync(entity);

        return true;
    }

    public Task<bool> AddOrUpdateEntityAsync<T>(T entity, Func<bool> condition) where T : class
    {
        if (condition())
        {
            return AddEntityAsync(entity);
        }
        else
        {
            return UpdateEntityAsync(entity);
        }
    }

    public async Task<bool> RemoveEntityAsync<T>(T entity) where T : class
    {
        var result = await _repository.RemoveEntityAsync(entity);

        if (!result) return false;

        await _cacheService.ClearCacheKeyAsync(GetCacheKey(entity));

        return true;
    }

    public Task<int> SaveChangesAsync() => _repository.SaveChangesAsync();

    protected virtual Task SetCacheAsync(object entity)
    {
        return _cacheService.SetCacheValueAsync(GetCacheKey(entity), ConvertBeforeCaching(entity));
    }

    protected abstract string GetCacheKey(object entity);

    protected virtual object ConvertBeforeCaching(object entity) => entity;
}
