using Item_Trading_App_REST_API.Data;
using Item_Trading_App_REST_API.Entities;
using Item_Trading_App_REST_API.Models.Identity;
using Item_Trading_App_REST_API.Options;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.Identity
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly JwtSettings _jwtSettings;
        private readonly UserManager<User> _userManager;
        private readonly DatabaseContext _context;

        public RefreshTokenService(JwtSettings jwtSettings, UserManager<User> userManager, DatabaseContext context)
        {
            _jwtSettings = jwtSettings;
            _userManager = userManager;
            _context = context;
        }

        public async Task<RefreshTokenResult> GenerateRefreshToken(string userId, string jti)
        {
            if(string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(jti))
            {
                return new RefreshTokenResult { Errors = new[] { "Invalid input data" } };
            }

            var user = await _userManager.FindByIdAsync(userId);

            if(user == null)
            {
                return new RefreshTokenResult { Errors = new[] { "User not found" } };
            }

            var refreshToken = new RefreshToken
            {
                Token = Guid.NewGuid().ToString(),
                JwtId = jti,
                UserId = user.Id,
                CreationDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.Add(_jwtSettings.RefreshTokenLifetime),

            };

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            return new RefreshTokenResult
            {
                Success = true,
                Token = refreshToken.Token,
                UserId = refreshToken.UserId,
                Used = refreshToken.Used,
                CreationDate = refreshToken.CreationDate,
                ExpiryDate = refreshToken.ExpiryDate,
                Invalidated = refreshToken.Invalidated
            };
        }

        public async Task<RefreshTokenResult> GetRefreshToken(string refreshTokenId)
        {
            var tmp = _context.RefreshTokens.FirstOrDefault(x => Equals(x.Token, refreshTokenId));

            if (tmp == null || tmp == default)
                return null;

            return new RefreshTokenResult
            {
                Token = tmp.Token,
                UserId = tmp.UserId,
                CreationDate = tmp.CreationDate,
                ExpiryDate = tmp.ExpiryDate,
                Invalidated = tmp.Invalidated,
                Used = tmp.Used,
                Success = true
            };
        }

        public async Task<bool> RemoveRefreshToken(string refreshTokenId)
        {
            DeleteRefreshToken(refreshTokenId);

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task ClearRefreshTokensAsync()
        {
            var expiredTokens = await GetExpiredRefreshTokens();

            if(expiredTokens != null)
            {
                foreach(var token in expiredTokens)
                {
                    DeleteRefreshToken(token);
                }
            }

            await _context.SaveChangesAsync();

            var usedTokens = await GetUsedRefreshTokens();

            if(usedTokens != null)
            {
                foreach(var token in usedTokens)
                {
                    DeleteRefreshToken(token);
                }
            }

            await _context.SaveChangesAsync();

            var usersId = _context.Users.Select(x => x.Id).ToList();

            if (usersId == null)
                return;

            foreach(string userId in usersId)
            {
                var tokens = _context.RefreshTokens.Where(x => Equals(x.UserId, userId)).ToList();

                if (tokens == null)
                    continue;

                if (tokens.Count <= _jwtSettings.AllowedRefreshTokensPerUser)
                    continue;

                tokens = tokens.OrderBy(x => x.CreationDate.Ticks).ToList();

                int n = tokens.Count - _jwtSettings.AllowedRefreshTokensPerUser; // number of tokens to be deleted

                if (n < 1)
                    continue;

                for (int i = 0; i < n; i++)
                    _context.RefreshTokens.Remove(tokens[i]);

                await _context.SaveChangesAsync();
            }
        }

        public async Task<RefreshTokenResult> GetRecentRefreshToken(string userId, string jti)
        {
            var tmp = _context.RefreshTokens.Where(x => Equals(x.UserId, userId) && x.Used == false && x.ExpiryDate > DateTime.UtcNow.AddHours(1) && x.Invalidated == false && Equals(x.JwtId, jti));
            if(tmp == null || tmp.Count() == 0)
            {
                return new RefreshTokenResult
                {
                    Errors = new[] { "No refresh token found" }
                };
            }

            tmp = tmp.OrderBy(x => x.CreationDate);

            var list = tmp.ToList();

            var item = list[list.Count - 1];

            return new RefreshTokenResult
            {
                Success = true,
                Token = item.Token,
                UserId = userId,
                CreationDate = item.CreationDate,
                ExpiryDate = item.ExpiryDate,
                Used = item.Used,
                Invalidated = item.Invalidated
            };
        }

        private async Task<List<string>> GetExpiredRefreshTokens()
        {
            return _context.RefreshTokens.Where(x => x.ExpiryDate < DateTime.UtcNow).Select(x => x.Token).ToList();
        }

        private async Task<List<string>> GetUsedRefreshTokens()
        {
            return _context.RefreshTokens.Where(x => x.Used).Select(x => x.Token).ToList();
        }

        private void DeleteRefreshToken(string refreshToken)
        {
            // we have to get the entity in order to delete by id
            var entity = GetRefreshTokenEntity(refreshToken);

            if (entity == null)
                return;

            _context.RefreshTokens.Remove(entity);
        }

        private Entities.RefreshToken GetRefreshTokenEntity(string refreshTokenId) => _context.RefreshTokens.Find(refreshTokenId);
    }
}
