using Item_Trading_App_REST_API.Data;
using Item_Trading_App_REST_API.Entities;
using Item_Trading_App_REST_API.Models.Identity;
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

namespace Item_Trading_App_REST_API.Services.Identity;

public class IdentityService : IIdentityService
{
    private readonly UserManager<User> _userManager;
    private readonly JwtSettings _jwtSettings;
    private readonly DatabaseContext _context;
    private readonly TokenValidationParameters _tokenValidationParameters;
    private readonly IRefreshTokenService _refreshTokenService;

    public IdentityService(UserManager<User> userManager, JwtSettings jwtSettings, DatabaseContext context, TokenValidationParameters tokenValidationParameters, IRefreshTokenService refreshTokenService)
    {
        _userManager = userManager;
        _jwtSettings = jwtSettings;
        _context = context;
        _tokenValidationParameters = tokenValidationParameters;
        _refreshTokenService = refreshTokenService;
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

        var newUser = new User
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

        return await GetToken(newUser.Id);
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
        
        return await GetToken(user.Id);
    }

    public async Task<AuthenticationResult> RefreshTokenAsync(string token, string refreshToken)
    {
        var validatedToken = GetPrincipalFromToken(token);

        if (validatedToken == null)
        {
            return new AuthenticationResult { Errors = new[] { "Invalid token" } };
        }

        var jti = validatedToken.Claims.Single(x => x.Type == JwtRegisteredClaimNames.Jti).Value;

        var storedRefreshToken = await _context.RefreshTokens.SingleOrDefaultAsync(x => x.Token == refreshToken);

        if (storedRefreshToken == null)
        {
            return new AuthenticationResult { Errors = new[] { "This refresh token does not exist" } };
        }

        if (!Equals(storedRefreshToken.JwtId, jti))
        {
            return new AuthenticationResult { Errors = new[] { "This refresh token does not match the JWT" } };
        }

        var expiryDateUnix = long.Parse(validatedToken.Claims.Single(x => x.Type == JwtRegisteredClaimNames.Exp).Value);

        var expiryDateTimeUtc = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            .AddSeconds(expiryDateUnix);

        if (DateTime.UtcNow > storedRefreshToken.ExpiryDate)
        {
            return new AuthenticationResult { Errors = new[] { "This refresh token has expired" } };
        }

        if (storedRefreshToken.Invalidated)
        {
            return new AuthenticationResult { Errors = new[] { "This refresh token has been invalidated" } };
        }

        var user = await _userManager.FindByIdAsync(validatedToken.Claims.Single(x => x.Type == "id").Value);
        return await GetToken(user.Id);
    }

    public async Task<string> GetUsername(string userId)
    {
        if(string.IsNullOrEmpty(userId))
        {
            return "";
        }

        var user = await _userManager.FindByIdAsync(userId);

        if(user == null)
        {
            return "";
        }

        return user.UserName;
    }

    public async Task<UsersResult> ListUsers(string userId, string searchString)
    {
        var list = string.IsNullOrEmpty(searchString) ?
            await _context.Users.AsNoTracking().Select(u => u.Id).ToListAsync() :
            await _context.Users.AsNoTracking().Where(u => u.UserName.StartsWith(searchString)).Select(u => u.Id).ToListAsync();

        if (list == null)
        {
            return new UsersResult
            {
                Errors = new[] { "Something went wrong" }
            };
        }

        if (list.Contains(userId))
            list.Remove(userId);

        return new UsersResult
        {
            UsersId = list,
            Success = true
        };
    }

    private ClaimsPrincipal GetPrincipalFromToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        _tokenValidationParameters.ValidateLifetime = false;

        try
        {
            var principal = tokenHandler.ValidateToken(token, _tokenValidationParameters, out var validatedToken);

            _tokenValidationParameters.ValidateLifetime = true;

            if (!IsJwtWithValidSecurityAlgorithm(validatedToken))
            {
                return null;
            }

            return principal;
        }
        catch
        {
            _tokenValidationParameters.ValidateLifetime = true;

            return null;
        }
    }

    private static bool IsJwtWithValidSecurityAlgorithm(SecurityToken validatedToken)
    {
        return (validatedToken is JwtSecurityToken jwtSecurityToken) && jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase);
    }

    private async Task<AuthenticationResult> GetToken(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return new AuthenticationResult { Errors = new[] { "User not found" } };
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, await GetUsername(userId)),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("id", userId),
        };

        var userClaims = await _userManager.GetClaimsAsync(user);

        claims.AddRange(userClaims);

        var expirationTime = DateTime.UtcNow.Add(_jwtSettings.TokenLifetime);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expirationTime,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        
        var refreshToken = await GetRefreshToken(userId, token.Id);

        if(refreshToken == null)
        {
            return new AuthenticationResult
            {
                Errors = new[] { "Something went wrong" }
            };
        }

        return new AuthenticationResult
        {
            Success = true,
            Token = tokenHandler.WriteToken(token),
            RefreshToken = refreshToken.Token,
            ExpirationDateTime = expirationTime
        };
    }

    private async Task<RefreshTokenResult> GetRefreshToken(string userId, string jti)
    {
        var refreshToken = await _refreshTokenService.GetRecentRefreshToken(userId, jti);

        if(refreshToken != null && refreshToken.Success)
        {
            return refreshToken;
        }

        return await _refreshTokenService.GenerateRefreshToken(userId, jti);
    }
}
