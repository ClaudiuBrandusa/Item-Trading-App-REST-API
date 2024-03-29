﻿using Item_Trading_App_REST_API.Data;
using Item_Trading_App_REST_API.Entities;
using Item_Trading_App_REST_API.Models.Identity;
using Item_Trading_App_REST_API.Options;
using Item_Trading_App_REST_API.Resources.Commands.Identity;
using Item_Trading_App_REST_API.Resources.Queries.Identity;
using Item_Trading_App_REST_API.Services.DatabaseContextWrapper;
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

public class IdentityService : IIdentityService, IDisposable
{
    private readonly IDatabaseContextWrapper _databaseContextWrapper;
    private readonly DatabaseContext _context;
    private readonly UserManager<User> _userManager;
    private readonly JwtSettings _jwtSettings;
    private readonly TokenValidationParameters _tokenValidationParameters;
    private readonly IRefreshTokenService _refreshTokenService;

    public IdentityService(IDatabaseContextWrapper databaseContextWrapper, UserManager<User> userManager, JwtSettings jwtSettings, TokenValidationParameters tokenValidationParameters, IRefreshTokenService refreshTokenService)
    {
        _databaseContextWrapper = databaseContextWrapper;
        _context = databaseContextWrapper.ProvideDatabaseContext();
        _userManager = userManager;
        _jwtSettings = jwtSettings;
        _tokenValidationParameters = tokenValidationParameters;
        _refreshTokenService = refreshTokenService;
    }

    public async Task<AuthenticationResult> RegisterAsync(RegisterCommand model)
    {
        var user = await _userManager.FindByNameAsync(model.Username);

        if (user is not null)
            return new AuthenticationResult
            { 
                Errors = new[] { "User with this username already exists" }
            };

        if (string.IsNullOrEmpty(model.Username) || string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password))
            return new AuthenticationResult
            {
                Errors = new[] { "Invalid input data" }
            };

        var newUserId = Guid.NewGuid();

        var newUser = new User
        {
            Id = newUserId.ToString(),
            UserName = model.Username,
            Email = model.Email
        };

        var createdUser = await _userManager.CreateAsync(newUser, model.Password);

        if (!createdUser.Succeeded)
            return new AuthenticationResult
            { 
                Errors = createdUser.Errors.Select(x => x.Description)
            };

        return await GetToken(newUser.Id);
    }

    public async Task<AuthenticationResult> LoginAsync(LoginCommand model)
    {
        var user = await _userManager.FindByNameAsync(model.Username);

        if (user is null)
            return new AuthenticationResult
            { 
                Errors = new[] { "User does not exist" }
            };

        var userMatchPassword = await _userManager.CheckPasswordAsync(user, model.Password);

        if (!userMatchPassword)
            return new AuthenticationResult
            { 
                Errors = new[] { "Username or password is wrong" }
            };
        
        return await GetToken(user.Id);
    }

    public async Task<AuthenticationResult> RefreshTokenAsync(RefreshTokenCommand model)
    {
        var validatedToken = GetPrincipalFromToken(model.Token);

        if (validatedToken is null)
            return new AuthenticationResult { Errors = new[] { "Invalid token" } };

        var jti = validatedToken.Claims.Single(x => x.Type == JwtRegisteredClaimNames.Jti).Value;

        var storedRefreshToken = await _context.RefreshTokens.SingleOrDefaultAsync(x => x.Token == model.RefreshToken);

        if (storedRefreshToken is null)
            return new AuthenticationResult { Errors = new[] { "This refresh token does not exist" } };

        if (!Equals(storedRefreshToken.JwtId, jti))
            return new AuthenticationResult { Errors = new[] { "This refresh token does not match the JWT" } };

        var expiryDateUnix = long.Parse(validatedToken.Claims.Single(x => x.Type == JwtRegisteredClaimNames.Exp).Value);

        var expiryDateTimeUtc = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            .AddSeconds(expiryDateUnix);

        if (DateTime.UtcNow > storedRefreshToken.ExpiryDate)
            return new AuthenticationResult { Errors = new[] { "This refresh token has expired" } };

        if (storedRefreshToken.Invalidated)
            return new AuthenticationResult { Errors = new[] { "This refresh token has been invalidated" } };

        var user = await _userManager.FindByIdAsync(validatedToken.Claims.Single(x => x.Type == "id").Value);
        return await GetToken(user.Id);
    }

    public async Task<string> GetUsername(GetUsernameQuery model)
    {
        if (string.IsNullOrEmpty(model.UserId))
            return "";

        var dbContext = await _databaseContextWrapper.ProvideDatabaseContextAsync();

        var user = await dbContext.Users.FindAsync(model.UserId);

        if (user is null)
            return "";

        _databaseContextWrapper.Dispose(dbContext);

        return user.UserName;
    }

    public async Task<UsersResult> ListUsers(ListUsersQuery model)
    {
        var list = string.IsNullOrEmpty(model.SearchString) ?
            await _context.Users
                .AsNoTracking()
                .Select(u => u.Id)
                .ToListAsync()
                :
            await _context.Users
                .AsNoTracking()
                .Where(u => u.UserName.StartsWith(model.SearchString))
                .Select(u => u.Id)
                .ToListAsync();

        if (list is null)
            return new UsersResult
            {
                Errors = new[] { "Something went wrong" }
            };

        if (list.Contains(model.UserId))
            list.Remove(model.UserId);

        return new UsersResult
        {
            UsersId = list,
            Success = true
        };
    }

    public void Dispose()
    {
        _databaseContextWrapper.Dispose(_context);
        GC.SuppressFinalize(this);
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
                return null;

            return principal;
        }
        catch
        {
            _tokenValidationParameters.ValidateLifetime = true;

            return null;
        }
    }

    private static bool IsJwtWithValidSecurityAlgorithm(SecurityToken validatedToken) =>
        (validatedToken is JwtSecurityToken jwtSecurityToken) && jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase);

    private async Task<AuthenticationResult> GetToken(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
            return new AuthenticationResult { Errors = new[] { "User not found" } };

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);

        var claims = new List<Claim>
        {
            new (JwtRegisteredClaimNames.Sub, await GetUsername(new GetUsernameQuery { UserId = userId })),
            new (JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new ("id", userId),
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

        if (refreshToken is null)
            return new AuthenticationResult
            {
                Errors = new[] { "Something went wrong" }
            };

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

        if (refreshToken is not null && refreshToken.Success)
            return refreshToken;

        return await _refreshTokenService.GenerateRefreshToken(userId, jti);
    }
}
