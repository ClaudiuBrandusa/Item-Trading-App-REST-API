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

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
        .Build();
        var jwtSettings = new JwtSettings();
        configuration.Bind(nameof(JwtSettings), jwtSettings);

        var tokenValidationParameters = TestingUtils.GetTokenValidationParameters(jwtSettings.Secret);

        var refreshTokenServiceMock = new Mock<IRefreshTokenService>();
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

        _sut = new IdentityService(databaseContextWrapper, TestingUtils.GetUserManager(new UserStore<User>(_dbContext)), jwtSettings, tokenValidationParameters, refreshTokenServiceMock.Object);
    }

    [Theory(DisplayName = "Register user")]
    [InlineData("Test_Register_0", "Test@a.com", "Password123!")]
    [InlineData("Test_Register_1", "Test@a.com", "")]
    [InlineData("Test_Register_2", "", "Password123!")]
    [InlineData("", "", "")]
    public async void RegisterUser(string userName, string email, string password)
    {
        var result = await _sut.RegisterAsync(new RegisterCommand
        {
            Username = userName,
            Email = email,
            Password = password
        });

        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            Assert.False(result.Success, "The result should be unsuccessful");
        }
        else
        {
            Assert.True(result.Success, "The result should be successful");
            Assert.False(string.IsNullOrEmpty(result.Token));
            Assert.False(string.IsNullOrEmpty(result.RefreshToken));
        }
    }

    [Theory(DisplayName = "Login user")]
    [InlineData("Test_Login", "Password123!")]
    [InlineData("Test", "")]
    [InlineData("", "")]
    public async void LoginUser(string userName, string password)
    {
        await _sut.RegisterAsync(new RegisterCommand
        {
            Username = userName,
            Email = "Test@a.com",
            Password = password
        });

        var result = await _sut.LoginAsync(new LoginCommand
        {
            Username = userName,
            Password = password
        });

        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
        {
            Assert.False(result.Success, "The result should be unsuccessful");
        }
        else
        {
            Assert.True(result.Success, "The result should be successful");
            Assert.False(string.IsNullOrEmpty(result.Token));
            Assert.False(string.IsNullOrEmpty(result.RefreshToken));
        }
    }

    [Theory(DisplayName = "Refresh token")]
    [InlineData("Test_Refresh", "Password123!")]
    [InlineData("Test", "")]
    [InlineData("", "")]
    public async void RefreshToken(string userName, string password)
    {
        var registerResult = await _sut.RegisterAsync(new RegisterCommand
        {
            Username = userName,
            Email = "Test@a.com",
            Password = password
        });

        var result = await _sut.RefreshTokenAsync(new RefreshTokenCommand
        {
            Token = registerResult.Token,
            RefreshToken = registerResult.RefreshToken
        });

        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
        {
            Assert.False(result.Success, "The result should be unsuccessful");
        }
        else
        {
            Assert.True(result.Success, "The result should be successful");
            Assert.False(string.IsNullOrEmpty(result.Token));
            Assert.False(string.IsNullOrEmpty(result.RefreshToken));
        }
    }

    [Theory(DisplayName = "Login user")]
    [InlineData("Test_Login", "Password123!")]
    [InlineData("Test", "")]
    [InlineData("", "")]
    public async void GetUsername(string userName, string password)
    {
        var registerResult = await _sut.RegisterAsync(new RegisterCommand
        {
            Username = userName,
            Email = "Test@a.com",
            Password = password
        });

        await _dbContext.SaveChangesAsync();

        var userId = registerResult.Success ? (await _dbContext.Users.Where(x => x.UserName == userName).FirstOrDefaultAsync())!.Id : "";

        var result = await _sut.GetUsername(new GetUsernameQuery { UserId = userId });

        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
        {
            Assert.True(string.IsNullOrEmpty(result), "The username should be empty");
        }
        else
        {
            Assert.Equal(userName, userName);
        }
    }

    [Theory(DisplayName = "List users")]
    [InlineData("Test_Login", "Password123!", 4)]
    [InlineData("Test_Login_1", "Password123!", 1)]
    [InlineData("Test", "", 2)]
    [InlineData("", "", 1)]
    public async void ListUsers(string userName, string password, int count)
    {
        for (int i = 0; i < count; i++)
            await _sut.RegisterAsync(new RegisterCommand
            {
                Username = userName + $"+{i}",
                Email = "Test@a.com",
                Password = password
            });

        string userId = _dbContext.Users.FirstOrDefault()?.Id ?? "";

        var result = await _sut.ListUsers(new ListUsersQuery
        {
            SearchString = userName,
            UserId = userId
        });

        Assert.True(result.Success, "The result should be successful");
        if (!string.IsNullOrEmpty(userId))
            Assert.Equal(count - 1, result.UsersId.Count());
    }
}
