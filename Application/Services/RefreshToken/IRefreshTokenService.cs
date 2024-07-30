using Application.Results.RefreshToken;

namespace Application.Services.RefreshToken;

public interface IRefreshTokenService
{
    /// <summary>
    /// Generates a refresh token for the user with the given userId and jwtId
    /// </summary>
    Task<RefreshTokenResult> GenerateRefreshTokenAsync(string userId, string jti);

    /// <summary>
    /// Returns the refresh token with the given id
    /// </summary>
    Task<RefreshTokenResult> GetRefreshTokenAsync(string refreshTokenId);

    /// <summary>
    /// Returns the most recent refresh token
    /// </summary>
    Task<RefreshTokenResult> GetRecentRefreshTokenAsync(string userId, string jti);

    /// <summary>
    /// Removes the refresh token with the given id
    /// </summary>
    Task<bool> RemoveRefreshTokenAsync(string refreshTokenId);

    /// <summary>
    /// Clear the refresh tokens that we no longer need
    /// </summary>
    Task ClearRefreshTokensAsync();
}
