using Application.Behaviors.Identity.GetUsername;
using Application.Behaviors.Identity.ListUsers;
using Application.Behaviors.Identity.LoginUser;
using Application.Behaviors.Identity.RefreshToken;
using Application.Behaviors.Identity.RegisterUser;
using Application.Extensions;
using Application.Results.Identity;
using Application.Results.RefreshToken;
using Application.Services.RefreshToken;
using Application.Utils;
using Domain.Entities.Identity;
using Domain.Repositories.Identity;
using Item_Trading_App_REST_API.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Application.Services.Identity;

public class IdentityService : IIdentityService, IDisposable
{
    private readonly IIdentityRepository _repository;
    private readonly JwtSettings _jwtSettings;
    private readonly TokenValidationParameters _tokenValidationParameters;
    private readonly IRefreshTokenService _refreshTokenService;

    public IdentityService(IIdentityRepository repository, JwtSettings jwtSettings, TokenValidationParameters tokenValidationParameters, IRefreshTokenService refreshTokenService)
    {
        _repository = repository;
        _jwtSettings = jwtSettings;
        _tokenValidationParameters = tokenValidationParameters;
        _refreshTokenService = refreshTokenService;
    }

    public async Task<AuthenticationResult> RegisterAsync(RegisterCommand model)
    {
        var user = await _repository.GetUserByNameAsync(model.Username);

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

        var newUser = new User
        {
            Id = User.GenerateId(),
            UserName = model.Username,
            Email = model.Email
        };

        var createdUser = await _repository.CreateUserAsync(newUser, model.Password);

        if (!createdUser.Succeeded)
            return new AuthenticationResult
            {
                Errors = createdUser.Errors.Select(x => x.Description)
            };

        return await GetToken(newUser.Id);
    }

    public async Task<AuthenticationResult> LoginAsync(LoginCommand model)
    {
        var user = await _repository.GetUserByNameAsync(model.Username);

        if (user is null)
            return new AuthenticationResult
            {
                Errors = new[] { "User does not exist" }
            };

        var userMatchPassword = await _repository.CheckPasswordAsync(user, model.Password);

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

        var jti = validatedToken.GetJwtId();
        
        var storedRefreshToken = await _refreshTokenService.GetRefreshTokenAsync(model.RefreshToken);

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

        var user = await _repository.GetUserByIdAsync(validatedToken.Claims.Single(x => x.Type == "id").Value);

        if (user is null)
            return new AuthenticationResult { Errors = new[] { "User not found" } };

        return await GetToken(user.Id);
    }

    public Task<string> GetUsername(GetUsernameQuery model)
    {
        if (string.IsNullOrEmpty(model.UserId))
            return Task.FromResult(string.Empty);

        return _repository.GetUsernameAsync(model.UserId);
    }

    public async Task<UsersResult> ListUsers(ListUsersQuery model)
    {
        var list = await _repository.ListUsersAsync(model.SearchString);

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
        _repository.Dispose();
        GC.SuppressFinalize(this);
    }

    private ClaimsPrincipal? GetPrincipalFromToken(string token)
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
        validatedToken is JwtSecurityToken jwtSecurityToken && jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase);

    private async Task<AuthenticationResult> GetToken(string userId)
    {
        var user = await _repository.GetUserByIdAsync(userId);

        if (user is null)
            return new AuthenticationResult { Errors = new[] { "User not found" } };

        var tokenHandler = CreateJwtSecurityTokenHandler(user);
        
        var expirationTime = DateTimeUtils.DateTimeWithTimeSpanFromUtcNow(_jwtSettings.TokenLifetime);

        var tokenDescriptor = await CreateSecurityTokenDescriptor(user, expirationTime);//JwtUtils.CreateSecurityTokenDescriptor(claims, expirationTime, signingCredentials);

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

    private JwtSecurityTokenHandler CreateJwtSecurityTokenHandler(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        
        var key = JwtUtils.CreateKeyByteArrayFromJwtSecret(_jwtSettings.Secret);

        return tokenHandler;
    }

    private async Task<SecurityTokenDescriptor> CreateSecurityTokenDescriptor(User user, DateTime expirationTime)
    {
        var key = JwtUtils.CreateKeyByteArrayFromJwtSecret(_jwtSettings.Secret);

        var claims = JwtUtils.CreateUserJwtClaims(user);

        var userClaims = await _repository.GetClaimsAsync(user);

        claims.AddRange(userClaims);

        var signingCredentials = JwtUtils.CreateSigningCredentials(key);

        return JwtUtils.CreateSecurityTokenDescriptor(claims, expirationTime, signingCredentials);
    }

    private async Task<RefreshTokenResult> GetRefreshToken(string userId, string jti)
    {
        var refreshToken = await _refreshTokenService.GetRecentRefreshTokenAsync(userId, jti);

        if (refreshToken is not null && refreshToken.Success)
            return refreshToken;

        return await _refreshTokenService.GenerateRefreshTokenAsync(userId, jti);
    }
}
