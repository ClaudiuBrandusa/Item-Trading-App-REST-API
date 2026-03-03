using Application.Utils;
using Domain.Entities.Identity;
using Domain.Repositories.Identity;
using Infrastructure.Repositories.Identity;
using Infrastructure.Services.DatabaseContextWrapper;
using Infrastructure_IntegrationTests.Utils;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace Infrastructure_UnitTests.RepositoryTests;

public class RefreshTokenRepositoryTests
{
    private readonly IRefreshTokenRepository _sut;
    private readonly string DEFAULT_USER_ID = Guid.NewGuid().ToString();
    private readonly string DEFAULT_USER_NAME = "DefaultUsername";
    private readonly TimeSpan DEFAULT_TOKEN_LIFETIME = TimeSpan.FromSeconds(5);
    private readonly TimeSpan DEFAULT_REFRESH_TOKEN_LIFETIME = TimeSpan.FromSeconds(5);
    private const string JWT_SECRET = "0";

    private IDatabaseContextWrapper _contextWrapper;

    private readonly SecurityTokenDescriptor tokenDescriptor;

    public RefreshTokenRepositoryTests()
    {
        _contextWrapper = TestingUtils.GetDatabaseContextWrapper(Guid.NewGuid().ToString());
        var userManagerMock = TestingUtils.GetUserManager(new UserStore<User>(_contextWrapper.ProvideDatabaseContext()));

        var key = JwtUtils.CreateKeyByteArrayFromJwtSecret(JWT_SECRET);
        var signingCredentials = JwtUtils.CreateSigningCredentials(key);

        var claims = JwtUtils.CreateUserJwtClaims(DEFAULT_USER_ID, DEFAULT_USER_NAME);

        tokenDescriptor = JwtUtils.CreateSecurityTokenDescriptor(claims, DateTimeUtils.DateTimeWithTimeSpanFromUtcNow(DEFAULT_TOKEN_LIFETIME), signingCredentials);

        _sut = new RefreshTokenRepository(_contextWrapper, userManagerMock);
    }

    [Fact(DisplayName = "Generate a refresh token")]
    public async Task GenerateRefreshToken_GenerateANewRefreshToken_ReturnsTheGeneratedRefreshToken()
    {
        // Arrange

        var tokenHandler = new JwtSecurityTokenHandler();

        var token = tokenHandler.CreateToken(tokenDescriptor);

        // Act

        var generatedRefreshTokenResult = await _sut.GenerateRefreshTokenAsync(DEFAULT_USER_ID, token.Id, DEFAULT_REFRESH_TOKEN_LIFETIME);

        // Assert

        Assert.NotNull(generatedRefreshTokenResult);
        Assert.Equal(DEFAULT_USER_ID, generatedRefreshTokenResult.UserId);
        Assert.False(generatedRefreshTokenResult.Invalidated);
        Assert.False(generatedRefreshTokenResult.Used);
        Assert.Equal(token.Id, generatedRefreshTokenResult.JwtId);
    }

    [Fact(DisplayName = "Generate a refresh token and get the refresh token by id")]
    public async Task GetRefreshToken_GenerateANewRefreshTokenAndGetRefreshToken_ReturnsTheGeneratedRefreshToken()
    {
        // Arrange

        var tokenHandler = new JwtSecurityTokenHandler();

        var token = tokenHandler.CreateToken(tokenDescriptor);

        var generatedRefreshTokenResult = await _sut.GenerateRefreshTokenAsync(DEFAULT_USER_ID, token.Id, DEFAULT_REFRESH_TOKEN_LIFETIME);

        string refreshTokenId = generatedRefreshTokenResult.Token;

        // Act

        var refreshTokenResult = await _sut.GetRefreshTokenAsync(refreshTokenId);

        // Assert

        Assert.NotNull(refreshTokenResult);
        Assert.Equal(DEFAULT_USER_ID, refreshTokenResult.UserId);
        Assert.False(refreshTokenResult.Invalidated);
        Assert.False(refreshTokenResult.Used);
        Assert.Equal(token.Id, refreshTokenResult.JwtId);
    }

    [Fact(DisplayName = "Generate several refresh tokens and then list them")]
    public async Task ListRefreshTokens_GenerateSeveralRefreshTokensAndListThem_ReturnsAListWithGeneratedRefreshTokens()
    {
        // Arrange

        int count = 5;

        var tokenHandler = new JwtSecurityTokenHandler();

        var token = tokenHandler.CreateToken(tokenDescriptor);

        for (int i = 0; i < count; i++)
            await _sut.GenerateRefreshTokenAsync(DEFAULT_USER_ID, token.Id, DEFAULT_REFRESH_TOKEN_LIFETIME);

        // Act

        var listRefreshTokensResult = await _sut.ListRefreshTokensAsync(DEFAULT_USER_ID);

        // Assert

        Assert.NotNull(listRefreshTokensResult);
        Assert.Equal(count, listRefreshTokensResult.Length);
    }

    [Fact(DisplayName = "Generate a refresh token then delete the refresh token by id and get the refresh token by id")]
    public async Task DeleteRefreshToken_GenerateANewRefreshTokenThenDeleteTheRefreshTokenByIdAndGetRefreshToken_ReturnsTrue()
    {
        // Arrange

        var tokenHandler = new JwtSecurityTokenHandler();

        var token = tokenHandler.CreateToken(tokenDescriptor);

        var generatedRefreshTokenResult = await _sut.GenerateRefreshTokenAsync(DEFAULT_USER_ID, token.Id, DEFAULT_REFRESH_TOKEN_LIFETIME);

        string refreshTokenId = generatedRefreshTokenResult.Token;

        // Act

        var deleteRefreshTokenResult = await _sut.DeleteRefreshTokenAsync(refreshTokenId);

        var refreshTokenResult = await _sut.GetRefreshTokenAsync(refreshTokenId);

        // Assert

        Assert.True(deleteRefreshTokenResult);
        Assert.Null(refreshTokenResult);
    }

    [Fact(DisplayName = "Generate a refresh token then delete the refresh token and get the refresh token by id")]
    public async Task DeleteRefreshToken_GenerateANewRefreshTokenThenDeleteTheRefreshTokenAndGetRefreshToken_ReturnsTrue()
    {
        // Arrange

        var tokenHandler = new JwtSecurityTokenHandler();

        var token = tokenHandler.CreateToken(tokenDescriptor);

        var generatedRefreshTokenResult = await _sut.GenerateRefreshTokenAsync(DEFAULT_USER_ID, token.Id, DEFAULT_REFRESH_TOKEN_LIFETIME);

        string refreshTokenId = generatedRefreshTokenResult.Token;

        // Act

        var deleteRefreshTokenResult = await _sut.DeleteRefreshTokenAsync(generatedRefreshTokenResult);

        var refreshTokenResult = await _sut.GetRefreshTokenAsync(refreshTokenId);

        // Assert

        Assert.True(deleteRefreshTokenResult);
        Assert.Null(refreshTokenResult);
    }

    [Fact(DisplayName = "Generate a refresh token and return the last refresh token")]
    public async Task GetLastRefreshToken_GenerateANewRefreshTokenAndReturnTheLastRefreshToken_ReturnsTheLastRefreshToken()
    {
        // Arrange

        var tokenHandler = new JwtSecurityTokenHandler();

        var token = tokenHandler.CreateToken(tokenDescriptor);

        var generatedRefreshTokenResult = await _sut.GenerateRefreshTokenAsync(DEFAULT_USER_ID, token.Id, DEFAULT_REFRESH_TOKEN_LIFETIME);

        // Act

        var refreshTokenResult = await _sut.GetLastRefreshTokenAsync(DEFAULT_USER_ID, token.Id);

        // Assert

        Assert.NotNull(generatedRefreshTokenResult);
        Assert.Equal(DEFAULT_USER_ID, generatedRefreshTokenResult.UserId);
        Assert.False(generatedRefreshTokenResult.Invalidated);
        Assert.False(generatedRefreshTokenResult.Used);
        Assert.Equal(token.Id, generatedRefreshTokenResult.JwtId);
    }
}
