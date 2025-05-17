using Domain.Entities.Identity;

namespace Domain.Repositories;

public interface IRefreshTokenRepository : IRepository, IDisposable
{
    /// <summary>
    /// Generates a refresh token for the user with <paramref name="userId"/>
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="jwtTokenId"></param>
    /// <param name="refreshTokenLifetime"></param>
    Task<RefreshToken> GenerateRefreshTokenAsync(string userId, string jwtTokenId, TimeSpan refreshTokenLifetime);

    /// <param name="refreshTokenId"></param>
    /// <returns>Refresh token with <paramref name="refreshTokenId"/></returns>
    Task<RefreshToken?> GetRefreshTokenAsync(string refreshTokenId);

    /// <param name="userId"></param>
    /// <returns>User with <paramref name="userId"/></returns>
    Task<User?> GetUserAsync(string userId);

    /// <returns>User ids array with the registered users</returns>
    Task<string[]> ListUserIdsAsync();

    /// <param name="userId"></param>
    /// <returns>The refresh tokens of user with <paramref name="userId"/></returns>
    Task<RefreshToken[]> ListRefreshTokensAsync(string userId);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="refreshTokenId"></param>
    /// <returns></returns>
    Task<bool> DeleteRefreshTokenAsync(string refreshTokenId);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="refreshToken"></param>
    /// <returns></returns>
    Task<bool> DeleteRefreshTokenAsync(RefreshToken refreshToken);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="jwtTokenId"></param>
    /// <returns></returns>
    Task<RefreshToken?> GetLastRefreshTokenAsync(string userId, string jwtTokenId);

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    Task<string[]> ListExpiredRefreshTokenIdsAsync();

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    Task<string[]> ListUsedRefreshTokenIdsAsync();
}
