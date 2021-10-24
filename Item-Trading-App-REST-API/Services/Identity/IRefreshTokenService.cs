using Item_Trading_App_REST_API.Models.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.Identity
{
    public interface IRefreshTokenService
    {
        /// <summary>
        /// Generates a refresh token for the user with the given userId and jwtId
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="jwtId"></param>
        /// <returns></returns>
        Task<AuthenticationResult> GenerateRefreshToken(string userId);

        /// <summary>
        /// Returns the refresh token with the given id
        /// </summary>
        /// <param name="refreshTokenId"></param>
        /// <returns></returns>
        Task<RefreshTokenResult> GetRefreshToken(string refreshTokenId);

        /// <summary>
        /// Removes the refresh token with the given id
        /// </summary>
        /// <param name="refreshTokenId"></param>
        /// <returns></returns>
        Task<bool> RemoveRefreshToken(string refreshTokenId);
        
        /// <summary>
        /// Clear the refresh tokens that we no longer need
        /// </summary>
        /// <returns></returns>
        Task ClearRefreshTokensAsync();
    }
}
