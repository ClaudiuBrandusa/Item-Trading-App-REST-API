using Item_Trading_App_REST_API.Data;
using Item_Trading_App_REST_API.Entities;
using Item_Trading_App_REST_API.Models;
using Item_Trading_App_REST_API.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
    public class IdentityService : IIdentityService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly JwtSettings _jwtSettings;
        private readonly DatabaseContext _context;
        private readonly TokenValidationParameters _tokenValidationParameters;

        public IdentityService(UserManager<IdentityUser> userManager, JwtSettings jwtSettings, DatabaseContext context, TokenValidationParameters tokenValidationParameters)
        {
            _userManager = userManager;
            _jwtSettings = jwtSettings;
            _context = context;
            _tokenValidationParameters = tokenValidationParameters;
        }

        public async Task<AuthenticationResult> RegisterAsync(string username, string password)
        {
            var user = await _userManager.FindByNameAsync(username);

            if(user != null)
            {
                return new AuthenticationResult
                { 
                    Errors = new[] { "User with this username already exists" }
                };
            }

            var newUserId = Guid.NewGuid();

            var newUser = new IdentityUser
            {
                Id = newUserId.ToString(),
                UserName = username
            };

            var createdUser = await _userManager.CreateAsync(newUser, password);

            if(!createdUser.Succeeded)
            {
                return new AuthenticationResult
                { 
                    Errors = createdUser.Errors.Select(x => x.Description)
                };
            }

            return await GetAuthenticationResultForUser(newUser);
        }

        public async Task<AuthenticationResult> LoginAsync(string username, string password)
        {
            var user = await _userManager.FindByNameAsync(username);

            if(user == null)
            {
                return new AuthenticationResult
                { 
                    Errors = new[] { "User does not exist" }
                };
            }

            var userMatchPassword = await _userManager.CheckPasswordAsync(user, password);

            if(!userMatchPassword)
            {
                return new AuthenticationResult
                { 
                    Errors = new[] { "Username or password is wrong" }
                };
            }

            return await GetAuthenticationResultForUser(user);
        }

        public async Task<AuthenticationResult> RefreshTokenAsync(string token, string refreshToken)
        {
            var validatedToken = GetPrincipalFromToken(token);

            if (validatedToken == null)
            {
                return new AuthenticationResult { Errors = new[] { "Invalid token" } };
            }

            var expiryDateUnix = long.Parse(validatedToken.Claims.Single(x => x.Type == JwtRegisteredClaimNames.Exp).Value);

            var expiryDateTimeUtc = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                .AddSeconds(expiryDateUnix);

            if (expiryDateTimeUtc > DateTime.UtcNow)
            {
                return new AuthenticationResult { Errors = new[] { "This token hasn't expired yet" } };
            }

            var jti = validatedToken.Claims.Single(x => x.Type == JwtRegisteredClaimNames.Jti).Value;

            var storedRefreshToken = await _context.RefreshTokens.SingleOrDefaultAsync(x => x.Token == refreshToken);

            if (storedRefreshToken == null)
            {
                return new AuthenticationResult { Errors = new[] { "This refresh token does not exist" } };
            }

            if (DateTime.UtcNow > storedRefreshToken.ExpiryDate)
            {
                return new AuthenticationResult { Errors = new[] { "This refresh token has expired" } };
            }

            if (storedRefreshToken.Invalidated)
            {
                return new AuthenticationResult { Errors = new[] { "This refresh token has been invalidated" } };
            }

            if (storedRefreshToken.Used)
            {
                return new AuthenticationResult { Errors = new[] { "This refresh token has been used" } };
            }

            if (!Equals(storedRefreshToken.JwtId, jti))
            {
                return new AuthenticationResult { Errors = new[] { "This refresh token does not match the JWT" } };
            }

            storedRefreshToken.Used = true;
            _context.RefreshTokens.Update(storedRefreshToken);
            await _context.SaveChangesAsync();

            var user = await _userManager.FindByIdAsync(validatedToken.Claims.Single(x => x.Type == "id").Value);
            return await GetAuthenticationResultForUser(user);
        }

        private ClaimsPrincipal GetPrincipalFromToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                var principal = tokenHandler.ValidateToken(token, _tokenValidationParameters, out var validatedToken);
                if (!IsJwtWithValidSecurityAlgorithm(validatedToken))
                {
                    return null;
                }

                return principal;
            }
            catch
            {
                return null;
            }
        }

        private bool IsJwtWithValidSecurityAlgorithm(SecurityToken validatedToken)
        {
            return (validatedToken is JwtSecurityToken jwtSecurityToken) && jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase);
        }

        private async Task<AuthenticationResult> GetAuthenticationResultForUser(IdentityUser newUser)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, newUser.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("id", newUser.Id),
            };

            var userClaims = await _userManager.GetClaimsAsync(newUser);

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
                UserId = newUser.Id,
                CreationDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddMonths(6),

            };

            return new AuthenticationResult
            {
                Success = true,
                Token = tokenHandler.WriteToken(token),
                RefreshToken = refreshToken.Token
            };
        }
    }
}
