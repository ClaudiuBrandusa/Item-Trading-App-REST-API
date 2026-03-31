using Application.Options;
using Application.Services.RefreshToken;
using Application.Services.Identity;
using Application.Behaviors.Identity.RegisterUser;
using Application.Behaviors.Identity.LoginUser;
using Application.Behaviors.Identity.RefreshToken;
using Application.Behaviors.Identity.GetUsername;
using Application.Behaviors.Identity.ListUsers;
using Application.Services.Cache;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Domain.Entities.Identity;
using Application.Results.RefreshToken;
using Application.Utils;
using Domain.Repositories.Identity;
using Application.Models.Common;

namespace Application_UnitTests.Identity;

public class IdentityServiceTests
{
    private readonly IIdentityService _sut; // service under test
    private readonly List<User> collection;
    private readonly List<RefreshToken> refreshTokens;
    private readonly Dictionary<string, string> userIdPassword = new();

    public IdentityServiceTests()
    {
        collection = new List<User>();
        refreshTokens = new List<RefreshToken>();
        var refreshTokenServiceMock = new Mock<IRefreshTokenService>();
        var cacheServiceMock = new Mock<ICacheService>().Object;
        var identityRepositoryMock = TestingUtils.CreateRepositoryMock<User, IIdentityRepository>(collection);
        
        var jwtSettings = new JwtSettings
        {
            Secret = "test",
            TokenLifetime = TimeSpan.FromMinutes(5),
            RefreshTokenLifetime = TimeSpan.FromMinutes(20),
            AllowedRefreshTokensPerUser = 3
        };

        var tokenValidationParameters = JwtUtils.BuildTokenValidationParameters(jwtSettings.Secret);

        #region MediatorMocks

        refreshTokenServiceMock.Setup(x => x.GenerateRefreshTokenAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string userId, string jti) =>
            {
                var refreshToken = new RefreshToken(jti, userId, jwtSettings.RefreshTokenLifetime);

                refreshTokens.Add(refreshToken);
                identityRepositoryMock.Object.AddEntityAsync(refreshToken).Wait();
                
                return Result<RefreshTokenResult>.Success(new RefreshTokenResult
                {
                    Token = refreshToken.Token,
                    UserId = userId,
                    Used = refreshToken.Used,
                    CreationDate = refreshToken.CreationDate,
                    ExpiryDate = refreshToken.ExpiryDate,
                    Invalidated = refreshToken.Invalidated
                });
            });

        refreshTokenServiceMock.Setup(repo => repo.GetRefreshTokenAsync(It.IsAny<string>()))
            .ReturnsAsync((string refreshTokenId) =>
            {
                var refreshToken = refreshTokens.FirstOrDefault(x => x.Token == refreshTokenId);

                if (refreshToken is null) return Result<RefreshTokenResult>.Failure("Something went wrong");

                return Result<RefreshTokenResult>.Success(new RefreshTokenResult
                {
                    Token = refreshToken.Token,
                    JwtId = refreshToken.JwtId,
                    UserId = refreshToken.UserId,
                    Used = refreshToken.Used,
                    CreationDate = refreshToken.CreationDate,
                    ExpiryDate = refreshToken.ExpiryDate,
                    Invalidated = refreshToken.Invalidated
                });
            });

        identityRepositoryMock.Setup(repo => repo.CreateUserAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync((User user, string password) =>
            {
                collection.Add(user);
                userIdPassword.Add(user.Id, password);

                return IdentityResult.Success;
            });

        identityRepositoryMock.Setup(repo => repo.GetUserByNameAsync(It.IsAny<string>()))
            .ReturnsAsync((string username) =>
            {
                return GetUserByName(username);
            });

        identityRepositoryMock.Setup(repo => repo.CheckPasswordAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync((User user, string password) =>
            {
                return GetUserPassword(user.Id) == password;
            });

        identityRepositoryMock.Setup(repo => repo.GetUserByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string userId) =>
            {
                return GetUserById(userId);
            });

        identityRepositoryMock.Setup(repo => repo.GetUsernameAsync(It.IsAny<string>()))
            .ReturnsAsync((string userId) =>
            {
                return GetUserById(userId)?.UserName ?? string.Empty;
            });

        identityRepositoryMock.Setup(repo => repo.GetClaimsAsync(It.IsAny<User>()))
            .ReturnsAsync((User user) =>
            {
                return new List<Claim>();
            });

        identityRepositoryMock.Setup(repo => repo.ListUsersAsync(It.IsAny<string>()))
            .ReturnsAsync((string searchString) =>
            {
                if (string.IsNullOrEmpty(searchString))
                    return collection.Select(x => x.Id ?? string.Empty).ToList();

                return collection.Where(x => !string.IsNullOrEmpty(x.UserName) && x.UserName.StartsWith(searchString)).Select(x => x.Id ?? string.Empty).ToList();
            });

        #endregion MediatorMocks

        _sut = new IdentityService(identityRepositoryMock.Object, jwtSettings, tokenValidationParameters, refreshTokenServiceMock.Object);
    }

    [Fact(DisplayName = "Register user")]
    public async Task Register_RegisterUserWithValidData_ReturnsSuccessfulAuthenticationResult()
    {
        // Arrange

        string userName = "Test_Register_0";
        string email = "Test@a.com";
        string password = "Password123!";

        var commandStub = new RegisterCommand
        {
            Username = userName,
            Email = email,
            Password = password
        };

        // Act

        var result = await _sut.RegisterAsync(commandStub);

        // Assert

        Assert.True(result.IsSuccess, "The result should be successful");
        Assert.NotNull(result.Content);
        var retrievedContent = result.Content;
        Assert.False(string.IsNullOrEmpty(retrievedContent.Token), "The token must not be empty");
        Assert.False(string.IsNullOrEmpty(retrievedContent.RefreshToken), "The refresh token must not be empty");
    }

    [Theory(DisplayName = "Register user with invalid data")]
    [InlineData("Test_Register_1", "Test@a.com", "")]
    [InlineData("Test_Register_2", "", "Password123!")]
    [InlineData("", "", "")]
    public async Task Register_RegisterUserWithInvalidData_ShouldFail(string userName, string email, string password)
    {
        // Arrange

        var commandStub = new RegisterCommand
        {
            Username = userName,
            Email = email,
            Password = password
        };

        // Act

        var result = await _sut.RegisterAsync(commandStub);

        // Assert

        Assert.False(result.IsSuccess, "The result should be unsuccessful");
    }

    [Fact(DisplayName = "Login user")]
    public async Task Login_LoginUserWithValidData_ReturnsSuccessfulAuthenticationResult()
    {
        // Arrange

        string userName = "Test_Login";
        string email = "Test@a.com";
        string password = "Password123!";

        var commandStub = new RegisterCommand
        {
            Username = userName,
            Email = email,
            Password = password
        };

        // Act

        await _sut.RegisterAsync(commandStub);

        var result = await _sut.LoginAsync(new LoginCommand
        {
            Username = userName,
            Password = password
        });

        // Assert

        Assert.True(result.IsSuccess, "The result should be successful");
        Assert.NotNull(result.Content);
        var retrievedContent = result.Content;
        Assert.False(string.IsNullOrEmpty(retrievedContent.Token), "The token must not be empty");
        Assert.False(string.IsNullOrEmpty(retrievedContent.RefreshToken), "The refresh token must not be empty");
    }


    [Theory(DisplayName = "Login user with invalid data")]
    [InlineData("Test", "")]
    [InlineData("", "")]
    public async Task Login_LoginUserWithInvalidData_ShouldFail(string userName, string password)
    {
        // Arrange

        var commandStub = new RegisterCommand
        {
            Username = userName,
            Email = "Test@a.com",
            Password = password
        };

        // Act

        await _sut.RegisterAsync(commandStub);

        var result = await _sut.LoginAsync(new LoginCommand
        {
            Username = userName,
            Password = password
        });

        // Assert

        Assert.False(result.IsSuccess, "The result should be unsuccessful");
    }

    [Fact(DisplayName = "Refresh token")]
    public async Task RefreshToken_RefreshTokenWithValidData_ReturnsSuccessfulAuthenticationResult()
    {
        // Arrange

        string userName = "Test_Refresh";
        string password = "Password123!";

        var registerCommandStub = new RegisterCommand
        {
            Username = userName,
            Email = "Test@a.com",
            Password = password
        };

        var registerResult = await _sut.RegisterAsync(registerCommandStub);

        var refreshTokenCommandStub = new RefreshTokenCommand
        {
            Token = registerResult.Content!.Token,
            RefreshToken = registerResult.Content.RefreshToken
        };

        // Act

        var result = await _sut.RefreshTokenAsync(refreshTokenCommandStub);

        // Assert

        Assert.True(result.IsSuccess, "The result should be successful");
        Assert.NotNull(result.Content);
        var retrievedContent = result.Content;
        Assert.False(string.IsNullOrEmpty(retrievedContent.Token), "The token must not be empty");
        Assert.False(string.IsNullOrEmpty(retrievedContent.RefreshToken), "The refresh token must not be empty");
    }


    [Theory(DisplayName = "Refresh token with invalid data")]
    [InlineData("Test", "")]
    [InlineData("", "")]
    public async Task RefreshToken_RefreshTokenWithInvalidData_ShouldFail(string userName, string password)
    {
        // Arrange

        var registerCommandStub = new RegisterCommand
        {
            Username = userName,
            Email = "Test@a.com",
            Password = password
        };

        var registerResult = await _sut.RegisterAsync(registerCommandStub);

        var token = string.Empty;
        var refreshToken = string.Empty;

        if (registerResult.IsSuccess)
        {
            token = registerResult.Content!.Token;
            refreshToken = registerResult.Content.RefreshToken;
        }

        var refreshTokenCommandStub = new RefreshTokenCommand
        {
            Token = token,
            RefreshToken = refreshToken
        };

        // Act

        var result = await _sut.RefreshTokenAsync(refreshTokenCommandStub);

        // Assert

        Assert.False(result.IsSuccess, "The result should be unsuccessful");
    }

    [Fact(DisplayName = "Get username")]
    public async Task GetUsername_RegisterUserThenGetUsername_ReturnsRegisteredUsersName()
    {
        // Arrange

        string userName = "Test_Login";
        string password = "Password123!";

        var registerCommandStub = new RegisterCommand
        {
            Username = userName,
            Email = "Test@a.com",
            Password = password
        };

        await _sut.RegisterAsync(registerCommandStub);

        var userId = GetUserByName(userName)?.Id ?? string.Empty;

        var usernameQueryStub = new GetUsernameQuery { UserId = userId };

        // Act

        var result = await _sut.GetUsername(usernameQueryStub);

        // Assert
        
        Assert.Equal(userName, result);
    }

    [Fact(DisplayName = "Get username without registering the user")]
    public async Task GetUsername_GetUsernameWithoutRegisteringAnUser_ShouldFail()
    {
        // Arrange

        var userId = string.Empty;

        var usernameQueryStub = new GetUsernameQuery { UserId = userId };

        // Act

        var result = await _sut.GetUsername(usernameQueryStub);

        // Assert

        Assert.True(string.IsNullOrEmpty(result), "The username should be empty");
    }

    [Theory(DisplayName = "List users")]
    [InlineData("Test_Login", "Password123!", 4)]
    [InlineData("Test_Login_1", "Password123!", 1)]
    public async Task ListUsers_RegisterSeveralUsersThenListTheUserIds_ReturnsRegisteredUserIdsList(string userName, string password, int count)
    {
        // Arrange

        var usernameFormula = (int number) => userName + $"+{number}";

        for (int i = 0; i < count; i++)
        {
            await _sut.RegisterAsync(new RegisterCommand
            {
                Username = usernameFormula(i),
                Email = "Test@a.com",
                Password = password
            });
        }

        string userId = GetUserByName(usernameFormula(0))?.Id ?? string.Empty;

        var listUsersQueryStub = new ListUsersQuery
        {
            SearchString = userName,
            UserId = userId
        };

        // Act

        var result = await _sut.ListUsers(listUsersQueryStub);

        // Assert

        Assert.True(result.IsSuccess, "The result should be successful");
        Assert.NotNull(result.Content);
        var retrievedContent = result.Content;
        Assert.Equal(count - 1, retrievedContent.UsersId.Count());
    }

    [Theory(DisplayName = "List users with invalid data")]
    [InlineData("Test", "", 2)]
    [InlineData("", "", 1)]
    public async Task ListUsers_RegisterSeveralUsersWithInvalidData_ShouldFail(string userName, string password, int count)
    {
        // Arrange

        var usernameFormula = (int number) => userName + $"+{number}";

        for (int i = 0; i < count; i++)
            await _sut.RegisterAsync(new RegisterCommand
            {
                Username = usernameFormula(i),
                Email = "Test@a.com",
                Password = password
            });

        string userId = GetUserByName(usernameFormula(0))?.Id ?? string.Empty;

        var listUsersQueryStub = new ListUsersQuery
        {
            SearchString = userName,
            UserId = userId
        };

        // Act

        var result = await _sut.ListUsers(listUsersQueryStub);

        // Assert

        Assert.True(result.IsSuccess, "The result should be successful");
        Assert.NotNull(result.Content);
        var retrievedContent = result.Content;
        Assert.Empty(retrievedContent.UsersId);
    }

    #region Utils

    private User? GetUserById(string userId)
    {
        return collection.FirstOrDefault(x => x.Id == userId);
    }

    private User? GetUserByName(string userName)
    {
        return collection.FirstOrDefault(x => x.UserName == userName);
    }

    private string GetUserPassword(string userId)
    {
        if (!userIdPassword.ContainsKey(userId)) return string.Empty;
        return userIdPassword[userId];
    }

    #endregion Utils
}
