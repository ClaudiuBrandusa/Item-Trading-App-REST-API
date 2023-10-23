using Item_Trading_App_REST_API.Models.Identity;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.Identity;

public interface IRefreshTokenService
{
    /// <summary>
    /// Generates a refresh token for the user with the given userId and jwtId
    /// </summary>
    Task<RefreshTokenResult> GenerateRefreshToken(string userId, string jti);

    /// <summary>
    /// Returns the refresh token with the given id
    /// </summary>
    Task<RefreshTokenResult> GetRefreshToken(string refreshTokenId);

    /// <summary>
    /// Returns the most recent refresh token
    /// </summary>
    Task<RefreshTokenResult> GetRecentRefreshToken(string userId, string jti);

    /// <summary>
    /// Removes the refresh token with the given id
    /// </summary>
    Task<bool> RemoveRefreshToken(string refreshTokenId);
    
    /// <summary>
    /// Clear the refresh tokens that we no longer need
    /// </summary>
    Task ClearRefreshTokensAsync();
}
