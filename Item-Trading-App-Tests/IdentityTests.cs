using Item_Trading_App_REST_API.Resources.Commands.Identity;
using Item_Trading_App_REST_API.Data;
using Item_Trading_App_REST_API.Entities;
using Item_Trading_App_REST_API.Models.Identity;
using Item_Trading_App_REST_API.Options;
using Item_Trading_App_REST_API.Resources.Queries.Identity;
using Item_Trading_App_REST_API.Services.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Item_Trading_App_Tests;

public class IdentityTests
{
    private readonly IIdentityService _sut; // service under test
    private readonly DatabaseContext _dbContext;

    public IdentityTests()
    {
        var databaseContextWrapper = TestingUtils.GetDatabaseContextWrapper(Guid.NewGuid().ToString());
        _dbContext = databaseContextWrapper.ProvideDatabaseContext();
        var refreshTokenServiceMock = new Mock<IRefreshTokenService>();

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
        .Build();
        var jwtSettings = new JwtSettings();
        configuration.Bind(nameof(JwtSettings), jwtSettings);

        var tokenValidationParameters = TestingUtils.GetTokenValidationParameters(jwtSettings.Secret);

        #region MediatorMocks

        refreshTokenServiceMock.Setup(x => x.GenerateRefreshToken(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string userId, string jti) =>
            {
                var refreshToken = new RefreshToken
                {
                    Token = Guid.NewGuid().ToString(),
                    JwtId = jti,
                    UserId = userId,
                    CreationDate = DateTime.UtcNow,
                    ExpiryDate = DateTime.UtcNow.Add(jwtSettings.RefreshTokenLifetime)
                };

                _dbContext.AddEntityAsync(refreshToken).Wait();
                
                return new RefreshTokenResult
                {
                    Success = true,
                    Token = refreshToken.Token,
                    UserId = userId,
                    Used = refreshToken.Used,
                    CreationDate = refreshToken.CreationDate,
                    ExpiryDate = refreshToken.ExpiryDate,
                    Invalidated = refreshToken.Invalidated
                };
            });
        refreshTokenServiceMock.Setup(x => x.GetRecentRefreshToken(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string userId, string jti) =>
            {
                var entity = _dbContext.RefreshTokens.AsNoTracking().Where(x => x.UserId == userId).LastOrDefault();

                if (entity is null)
                    return new RefreshTokenResult();

                return new RefreshTokenResult
                {
                    Success = true,
                    Token = entity.Token,
                    UserId = userId,
                    CreationDate = entity.CreationDate,
                    ExpiryDate = entity.ExpiryDate,
                    Used = entity.Used,
                    Invalidated = entity.Invalidated
                };
            });

        #endregion MediatorMocks

        _sut = new IdentityService(databaseContextWrapper, TestingUtils.GetUserManager(new UserStore<User>(_dbContext)), jwtSettings, tokenValidationParameters, refreshTokenServiceMock.Object);
    }

    [Fact(DisplayName = "Register user")]
    public async Task RegisterUser()
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

        Assert.True(result.Success, "The result should be successful");
        Assert.False(string.IsNullOrEmpty(result.Token), "The token must not be empty");
        Assert.False(string.IsNullOrEmpty(result.RefreshToken), "The refresh token must not be empty");
    }

    [Theory(DisplayName = "Register user with invalid data")]
    [InlineData("Test_Register_1", "Test@a.com", "")]
    [InlineData("Test_Register_2", "", "Password123!")]
    [InlineData("", "", "")]
    public async Task RegisterUserWithInvalidData(string userName, string email, string password)
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

        Assert.False(result.Success, "The result should be unsuccessful");
    }

    [Fact(DisplayName = "Login user")]
    public async Task LoginUser()
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

        Assert.True(result.Success, "The result should be successful");
        Assert.False(string.IsNullOrEmpty(result.Token), "The token must not be empty");
        Assert.False(string.IsNullOrEmpty(result.RefreshToken), "The refresh token must not be empty");
    }


    [Theory(DisplayName = "Login user with invalid data")]
    [InlineData("Test", "")]
    [InlineData("", "")]
    public async Task LoginUserWithInvalidData(string userName, string password)
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

        Assert.False(result.Success, "The result should be unsuccessful");
    }

    [Fact(DisplayName = "Refresh token")]
    public async Task RefreshToken()
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
            Token = registerResult.Token,
            RefreshToken = registerResult.RefreshToken
        };

        // Act

        var result = await _sut.RefreshTokenAsync(refreshTokenCommandStub);

        // Assert

        Assert.True(result.Success, "The result should be successful");
        Assert.False(string.IsNullOrEmpty(result.Token), "The token must not be empty");
        Assert.False(string.IsNullOrEmpty(result.RefreshToken), "The refresh token must not be empty");
    }


    [Theory(DisplayName = "Refresh token with invalid data")]
    [InlineData("Test", "")]
    [InlineData("", "")]
    public async Task RefreshTokenWithInvalidData(string userName, string password)
    {
        // Arrange

        var registerCommandStub = new RegisterCommand
        {
            Username = userName,
            Email = "Test@a.com",
            Password = password
        };

        var registerResult = await _sut.RegisterAsync(registerCommandStub);

        var refreshTokenCommandStub = new RefreshTokenCommand
        {
            Token = registerResult.Token,
            RefreshToken = registerResult.RefreshToken
        };

        // Act

        var result = await _sut.RefreshTokenAsync(refreshTokenCommandStub);

        // Assert

        Assert.False(result.Success, "The result should be unsuccessful");
    }

    [Fact(DisplayName = "Get username")]
    public async Task GetUsername()
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

        var registerResult = await _sut.RegisterAsync(registerCommandStub);

        await _dbContext.SaveChangesAsync();

        var userId = registerResult.Success ? (await _dbContext.Users.Where(x => x.UserName == userName).FirstOrDefaultAsync())!.Id : "";

        var usernameQueryStub = new GetUsernameQuery { UserId = userId };

        // Act

        var result = await _sut.GetUsername(usernameQueryStub);

        // Assert
        
        Assert.Equal(userName, result);
    }

    [Theory(DisplayName = "Get username with invalid data")]
    [InlineData("Test", "")]
    [InlineData("", "")]
    public async Task GetUsernameWithInvalidData(string userName, string password)
    {
        // Arrange

        var registerCommandStub = new RegisterCommand
        {
            Username = userName,
            Email = "Test@a.com",
            Password = password
        };

        var registerResult = await _sut.RegisterAsync(registerCommandStub);

        await _dbContext.SaveChangesAsync();

        var userId = registerResult.Success ? (await _dbContext.Users.Where(x => x.UserName == userName).FirstOrDefaultAsync())!.Id : "";

        var usernameQueryStub = new GetUsernameQuery { UserId = userId };

        // Act

        var result = await _sut.GetUsername(usernameQueryStub);

        // Assert

        Assert.True(string.IsNullOrEmpty(result), "The username should be empty");
    }

    [Theory(DisplayName = "List users")]
    [InlineData("Test_Login", "Password123!", 4)]
    [InlineData("Test_Login_1", "Password123!", 1)]
    public async Task ListUsers(string userName, string password, int count)
    {
        // Arrange

        for (int i = 0; i < count; i++)
            await _sut.RegisterAsync(new RegisterCommand
            {
                Username = userName + $"+{i}",
                Email = "Test@a.com",
                Password = password
            });

        string userId = _dbContext.Users.FirstOrDefault()?.Id ?? "";

        var listUsersQueryStub = new ListUsersQuery
        {
            SearchString = userName,
            UserId = userId
        };

        // Act

        var result = await _sut.ListUsers(listUsersQueryStub);

        // Assert

        Assert.True(result.Success, "The result should be successful");
        Assert.Equal(count - 1, result.UsersId.Count());
    }

    [Theory(DisplayName = "List users with invalid data")]
    [InlineData("Test", "", 2)]
    [InlineData("", "", 1)]
    public async Task ListUsersWithInvalidData(string userName, string password, int count)
    {
        // Arrange

        for (int i = 0; i < count; i++)
            await _sut.RegisterAsync(new RegisterCommand
            {
                Username = userName + $"+{i}",
                Email = "Test@a.com",
                Password = password
            });

        string userId = _dbContext.Users.FirstOrDefault()?.Id ?? "";

        var listUsersQueryStub = new ListUsersQuery
        {
            SearchString = userName,
            UserId = userId
        };

        // Act

        var result = await _sut.ListUsers(listUsersQueryStub);

        // Assert

        Assert.True(result.Success, "The result should be successful");
        Assert.Empty(result.UsersId);
    }
}
