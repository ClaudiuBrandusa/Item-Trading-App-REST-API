using Application.Utils;
using Domain.Entities.Identity;
using Domain.Repositories.Identity;
using Infrastructure.IntegrationTests.Common.Fixtures;
using Infrastructure.IntegrationTests.Utils;
using Infrastructure.Repositories.Identity;
using Infrastructure.Services.DatabaseContextWrapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace Infrastructure.IntegrationTests.Database;

public class RefreshTokenTests : IClassFixture<DatabaseFixture>
{
    private const string DEFAULT_USER_NAME = "Claudiu";
    private const string JWT_SECRET = "0";
    private readonly TimeSpan DEFAULT_REFRESH_TOKEN_LIFETIME = TimeSpan.FromSeconds(5);

    private readonly IRefreshTokenRepository _repository;
    private readonly IServiceProvider _serviceProvider;

    public RefreshTokenTests(DatabaseFixture fixture)
    {
        var dbContextWrapper = fixture.GetService<IDatabaseContextWrapper>();
        var userManager = fixture.GetService<UserManager<User>>();
        _repository = new RefreshTokenRepository(dbContextWrapper, userManager);
        _serviceProvider = fixture.ServiceProvider!;
    }

    [Fact]
    public async Task GenerateRefreshToken_CreateUserThenGenerateRefreshToken_ReturnsGeneratedRefreshToken()
    {
        // Arrange

        var user = await TestingScenarios.CreateUser(_serviceProvider, DEFAULT_USER_NAME, 0);
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = CreateSecurityTokenDescriptor(user);
        var token = tokenHandler.CreateToken(tokenDescriptor);

        // Act

        var refreshToken = await _repository.GenerateRefreshTokenAsync(user.Id, token.Id, DEFAULT_REFRESH_TOKEN_LIFETIME);

        // Assert

        Assert.NotNull(refreshToken);
        Assert.Equal(token.Id, refreshToken.JwtId);
        Assert.Equal(user.Id, refreshToken.UserId);
        Assert.False(refreshToken.Invalidated);
        Assert.False(refreshToken.Used);
    }

    [Fact]
    public async Task GetRefreshToken_CreateUserAndGenerateRefreshTokenThenGetTheRefreshToken_ReturnsTheRefreshToken()
    {
        // Arrange

        var user = await TestingScenarios.CreateUser(_serviceProvider, DEFAULT_USER_NAME, 1);
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = CreateSecurityTokenDescriptor(user);
        var token = tokenHandler.CreateToken(tokenDescriptor);

        // Act

        var refreshToken = await _repository.GenerateRefreshTokenAsync(user.Id, token.Id, DEFAULT_REFRESH_TOKEN_LIFETIME);
        var response = await _repository.GetRefreshTokenAsync(refreshToken.Token);

        // Assert

        Assert.NotNull(refreshToken);
        Assert.NotNull(response);
        Assert.Equal(token.Id, refreshToken.JwtId);
        Assert.Equal(user.Id, refreshToken.UserId);
        Assert.False(refreshToken.Invalidated);
        Assert.False(refreshToken.Used);
        Assert.Equal(refreshToken.Token, response.Token);
        Assert.Equal(refreshToken.JwtId, response.JwtId);
        Assert.Equal(refreshToken.UserId, response.UserId);
        Assert.Equal(refreshToken.Invalidated, response.Invalidated);
        Assert.Equal(refreshToken.Used, response.Used);
        Assert.Equal(refreshToken.CreationDate, response.CreationDate);
        Assert.Equal(refreshToken.ExpiryDate, response.ExpiryDate);
    }

    [Fact]
    public async Task GetUser_CreateUserThenGetTheCreatedUser_ReturnsCreatedUser()
    {
        // Arrange

        var user = await TestingScenarios.CreateUser(_serviceProvider, "Claudiu", 2);

        // Act

        var response = await _repository.GetUserAsync(user.Id);

        // Assert

        Assert.NotNull(response);
        Assert.Equal(user.Id, response.Id);
        Assert.Equal(user.UserName, response.UserName);
        Assert.Equal(user.Email, response.Email);
        Assert.Equal(user.Cash, response.Cash);
    }

    [Fact]
    public async Task ListUserIds_CreateSomeUsersThenListTheirIds_ReturnsAnArrayOfUserIds()
    {
        // Arrange

        var dbFixture = await DatabaseFixture.BuildDatabaseFixture();
        var dbContextWrapper = dbFixture.GetService<IDatabaseContextWrapper>();
        var userManager = dbFixture.GetService<UserManager<User>>();
        var serviceProvider = dbFixture.ServiceProvider!;
        var repository = new RefreshTokenRepository(dbContextWrapper, userManager);

        var users = await TestingScenarios.CreateUsers(serviceProvider, DEFAULT_USER_NAME, 3, 3);
        var expectedUserIds = users.Select(x => x.Id).ToArray();

        // Act

        var response = await repository.ListUserIdsAsync();

        // Assert

        Assert.NotNull(response);
        Assert.Equal(expectedUserIds.Length, response.Length);
        Assert.All(expectedUserIds, x => response.Contains(x));
    }

    [Fact]
    public async Task DeleteRefreshToken_byId_CreateUserAndGenerateRefreshTokenThenDeleteTheRefreshToken_ReturnsDeletionStatus()
    {
        // Arrange

        var user = await TestingScenarios.CreateUser(_serviceProvider, "Claudiu", 3);
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = CreateSecurityTokenDescriptor(user);
        var token = tokenHandler.CreateToken(tokenDescriptor);

        // Act

        var refreshToken = await _repository.GenerateRefreshTokenAsync(user.Id, token.Id, DEFAULT_REFRESH_TOKEN_LIFETIME);
        var response = await _repository.DeleteRefreshTokenAsync(refreshToken.Token);

        // Assert

        Assert.NotNull(refreshToken);
        Assert.True(response);
    }

    [Fact]
    public async Task DeleteRefreshToken_byEntity_CreateUserAndGenerateRefreshTokenThenDeleteTheRefreshToken_ReturnsDeletionStatus()
    {
        // Arrange

        var user = await TestingScenarios.CreateUser(_serviceProvider, "Claudiu", 4);
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = CreateSecurityTokenDescriptor(user);
        var token = tokenHandler.CreateToken(tokenDescriptor);

        // Act

        var refreshToken = await _repository.GenerateRefreshTokenAsync(user.Id, token.Id, DEFAULT_REFRESH_TOKEN_LIFETIME);
        var response = await _repository.DeleteRefreshTokenAsync(refreshToken);

        // Assert

        Assert.NotNull(refreshToken);
        Assert.True(response);
    }

    [Fact]
    public async Task GetLastRefreshToken_CreateUserAndGenerateRefreshTokenThenReturnTheLastRefreshToken_ReturnsTheNewestRefreshToken()
    {
        // Arrange

        var user = await TestingScenarios.CreateUser(_serviceProvider, "Claudiu", 5);
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = CreateSecurityTokenDescriptor(user);
        var token = tokenHandler.CreateToken(tokenDescriptor);

        // Act

        var firstRefreshToken = await _repository.GenerateRefreshTokenAsync(user.Id, token.Id, DEFAULT_REFRESH_TOKEN_LIFETIME);
        var lastRefreshToken = await _repository.GenerateRefreshTokenAsync(user.Id, token.Id, DEFAULT_REFRESH_TOKEN_LIFETIME);
        var response = await _repository.GetLastRefreshTokenAsync(user.Id, token.Id);

        // Assert

        Assert.NotNull(firstRefreshToken);
        Assert.NotNull(lastRefreshToken);
        Assert.NotNull(response);
        Assert.Equal(response.Token, lastRefreshToken.Token);
    }

    private SecurityTokenDescriptor CreateSecurityTokenDescriptor(User user)
    {
        var key = JwtUtils.CreateKeyByteArrayFromJwtSecret(JWT_SECRET);
        var claims = JwtUtils.CreateUserJwtClaims(user);
        var signingCredentials = JwtUtils.CreateSigningCredentials(key);
        var expirationTime = DateTimeUtils.DateTimeWithTimeSpanFromUtcNow(DEFAULT_REFRESH_TOKEN_LIFETIME);
        return JwtUtils.CreateSecurityTokenDescriptor(claims, expirationTime, signingCredentials);
    }
}
