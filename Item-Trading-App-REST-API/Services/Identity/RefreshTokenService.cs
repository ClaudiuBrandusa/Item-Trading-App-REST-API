using Item_Trading_App_REST_API.Data;
using Item_Trading_App_REST_API.Entities;
using Item_Trading_App_REST_API.Models.Identity;
using Item_Trading_App_REST_API.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
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

        public async Task<AuthenticationResult> GenerateRefreshToken(string userId)
        {
            if(string.IsNullOrEmpty(userId))
            {
                return new AuthenticationResult { Errors = new[] { "Invalid user id" } };
            }

            var user = await _userManager.FindByIdAsync(userId);

            if(user == null)
            {
                return new AuthenticationResult { Errors = new[] { "User not found" } };
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("id", user.Id),
            };

            var userClaims = await _userManager.GetClaimsAsync(user);

            claims.AddRange(userClaims);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.Add(_jwtSettings.TokenLifetime),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);

            var refreshToken = new RefreshToken
            {
                Token = Guid.NewGuid().ToString(),
                JwtId = token.Id,
                UserId = user.Id,
                CreationDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.Add(_jwtSettings.RefreshTokenLifetime),

            };

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            return new AuthenticationResult
            {
                Success = true,
                Token = tokenHandler.WriteToken(token),
                RefreshToken = refreshToken.Token
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

                while (tokens.Count > _jwtSettings.AllowedRefreshTokensPerUser) // leaves the last N refresh tokens created
                    _context.RefreshTokens.Remove(tokens[0]); // where N is the number of allowed refresh tokens per user
            }
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
