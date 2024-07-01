using Application.Services.Cache;
using Domain.Identity;
using Domain.Repositories;
using Infrastructure.Services.DatabaseContextWrapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;
public class RefreshTokenRepository : RepositoryBase, IRefreshTokenRepository
{
    private readonly UserManager<User> _userManager;

    public RefreshTokenRepository(IDatabaseContextWrapper databaseContextWrapper, ICacheService cacheService, UserManager<User> userManager) : base(databaseContextWrapper, cacheService)
    {
        _userManager = userManager;
    }

    public async Task<RefreshToken> GenerateRefreshTokenAsync(string userId, string jwtTokenId, TimeSpan refreshTokenLifetime)
    {
        var refreshToken = new RefreshToken
        {
            Token = Guid.NewGuid().ToString(),
            JwtId = jwtTokenId,
            UserId = userId,
            CreationDate = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.Add(refreshTokenLifetime)
        };

        await AddEntityAsync(refreshToken);

        return refreshToken;
    }

    public Task<RefreshToken?> GetRefreshTokenAsync(string refreshTokenId)
    {
        return context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == refreshTokenId);
    }

    public Task<User?> GetUserAsync(string userId)
    {
        return _userManager.FindByIdAsync(userId);
    }

    public Task<string[]> ListUserIdsAsync()
    {
        return context.Users.Select(x => x.Id).ToArrayAsync();
    }

    public Task<RefreshToken[]> ListRefreshTokensAsync(string userId)
    {
        return context.RefreshTokens.Where(x => Equals(x.UserId, userId)).ToArrayAsync();
    }

    public async Task<bool> DeleteRefreshTokenAsync(string refreshTokenId)
    {
        var entity = await GetRefreshTokenAsync(refreshTokenId);

        if (entity is null)
            return false;

        return await RemoveEntityAsync(entity);
    }

    public Task<bool> DeleteRefreshTokenAsync(RefreshToken refreshToken)
    {
        return RemoveEntityAsync(refreshToken);
    }

    public async Task<RefreshToken?> GetLastRefreshTokenAsync(string userId, string jwtTokenId)
    {
        var tmp = await context.RefreshTokens.AsNoTracking()
            .Where(x =>
                Equals(x.UserId, userId) &&
                x.Used == false &&
                x.ExpiryDate > DateTime.UtcNow.AddHours(1) &&
                x.Invalidated == false &&
                Equals(x.JwtId, jwtTokenId))
            .ToListAsync();

        if (tmp is null || !tmp.Any())
        {
            return null;
        }

        tmp = tmp.OrderBy(x => x.CreationDate).ToList();

        return tmp[^1];
    }

    public Task<string[]> ListExpiredRefreshTokenIdsAsync()
    {
        return context.RefreshTokens
            .Where(x => x.ExpiryDate < DateTime.UtcNow)
            .Select(x => x.Token)
            .ToArrayAsync();
    }

    public Task<string[]> ListUsedRefreshTokenIdsAsync()
    {
        return context.RefreshTokens
            .Where(x => x.Used)
            .Select(x => x.Token)
            .ToArrayAsync();
    }
}
