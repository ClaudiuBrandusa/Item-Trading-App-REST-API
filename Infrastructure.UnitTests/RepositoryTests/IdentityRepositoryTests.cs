using Domain.Entities.Identity;
using Domain.Repositories;
using Infrastructure.Data;
using Infrastructure.Repositories.Identity;
using Infrastructure.Services.DatabaseContextWrapper;
using Infrastructure_IntegrationTests.Utils;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Infrastructure_UnitTests.RepositoryTests;
public class IdentityRepositoryTests
{
    private readonly IIdentityRepository _sut;
    private const string DEFAULT_USER_NAME = "DefaultUserName";
    private const string DEFAULT_EMAIL = "Default@email.com";

    private IDatabaseContextWrapper _contextWrapper;

    public IdentityRepositoryTests()
    {
        _contextWrapper = TestingUtils.GetDatabaseContextWrapper(Guid.NewGuid().ToString());
        var userManagerMock = TestingUtils.GetUserManager(new UserStore<User>(_contextWrapper.ProvideDatabaseContext()));

        _sut = new IdentityRepository(_contextWrapper, userManagerMock);
    }

    [Fact(DisplayName = "Create user and check password")]
    public async Task CheckPassword_CreateUserAndCheckPassword_ReturnsTrue()
    {
        // Arrange

        var userMock = new User
        {
            Id = Guid.NewGuid().ToString(),
            UserName = DEFAULT_USER_NAME,
            Email = DEFAULT_EMAIL
        };

        string password = "!Ab12345";

        var userCreatedResult = await _sut.CreateUserAsync(userMock, password);

        // Act

        var checkPasswordResult = await _sut.CheckPasswordAsync(userMock, password);

        // Assert

        Assert.True(checkPasswordResult, "The password must match the user's password");
    }

    [Fact(DisplayName = "Create user and then return a list with its claims")]
    public async Task GetClaims_CreateUserAndThenReturnClaims_ReturnsClaims()
    {
        // Arrange

        var userMock = new User
        {
            Id = Guid.NewGuid().ToString(),
            UserName = DEFAULT_USER_NAME,
            Email = DEFAULT_EMAIL
        };

        string password = "!Ab12345";

        var userCreatedResult = await _sut.CreateUserAsync(userMock, password);

        // Act

        var claimsResult = await _sut.GetClaimsAsync(userMock);

        // Assert

        Assert.NotNull(claimsResult);
    }

    [Fact(DisplayName = "Create user then get the user by id")]
    public async Task GetUserById_CreateUserThenGetById_ReturnsUser()
    {
        // Arrange

        var userMock = new User
        {
            Id = Guid.NewGuid().ToString(),
            UserName = DEFAULT_USER_NAME,
            Email = DEFAULT_EMAIL
        };

        string password = "!Ab12345";

        var userCreatedResult = await _sut.CreateUserAsync(userMock, password);

        // Act

        var userResult = await _sut.GetUserByIdAsync(userMock.Id);

        // Assert

        Assert.NotNull(userResult);
        Assert.Equal(userMock.Id, userResult.Id);
        Assert.Equal(userMock.UserName, userResult.UserName);
        Assert.Equal(userMock.Email, userResult.Email);
    }

    [Fact(DisplayName = "Create user then get the user by name")]
    public async Task GetUserByName_CreateUserThenGetByName_ReturnsUser()
    {
        // Arrange

        var userMock = new User
        {
            Id = Guid.NewGuid().ToString(),
            UserName = DEFAULT_USER_NAME,
            Email = DEFAULT_EMAIL
        };

        string password = "!Ab12345";

        var userCreatedResult = await _sut.CreateUserAsync(userMock, password);

        // Act

        var userResult = await _sut.GetUserByNameAsync(userMock.UserName);

        // Assert

        Assert.NotNull(userResult);
        Assert.Equal(userMock.Id, userResult.Id);
        Assert.Equal(userMock.UserName, userResult.UserName);
        Assert.Equal(userMock.Email, userResult.Email);
    }

    [Fact(DisplayName = "Create user then return the username by user id")]
    public async Task GetUsername_CreateUserThenGetUsername_ReturnUsername()
    {
        // Arrange

        var userMock = new User
        {
            Id = Guid.NewGuid().ToString(),
            UserName = DEFAULT_USER_NAME,
            Email = DEFAULT_EMAIL
        };

        string password = "!Ab12345";

        var userCreatedResult = await _sut.CreateUserAsync(userMock, password);

        // Act

        var usernameResult = await _sut.GetUsernameAsync(userMock.Id);

        // Assert

        Assert.True(!string.IsNullOrEmpty(usernameResult), "Username must not be null or empty");
    }

    [Fact(DisplayName = "Create user then update")]
    public async Task UpdateUser_CreateUserThenUpdate_ReturnsUserUpdateResult()
    {
        // Arrange

        var userMock = new User
        {
            Id = Guid.NewGuid().ToString(),
            UserName = DEFAULT_USER_NAME,
            Email = DEFAULT_EMAIL
        };

        string password = "!Ab12345";

        var userCreatedResult = await _sut.CreateUserAsync(userMock, password);

        // Act

        string newUserName = userMock.UserName + "_NEW";

        userMock.UserName = newUserName;

        var userUpdatedResult = await _sut.UpdateUserAsync(userMock);

        var userResult = await _sut.GetUserByIdAsync(userMock.Id);

        // Assert

        Assert.True(userUpdatedResult, "User should have been updated");
        Assert.Equal(userMock.Id, userResult.Id);
        Assert.Equal(userMock.UserName, userResult.UserName);
        Assert.Equal(userMock.Email, userResult.Email);
    }

    [Fact(DisplayName = "Create several users then list the users")]
    public async Task ListUsers_CreateSeveralUsersThenListTheUsers_ReturnsUser()
    {
        // Arrange

        int count = 3;

        for (int i = 0; i < count; i++)
        {
            var userMock = new User
            {
                Id = Guid.NewGuid().ToString(),
                UserName = $"{DEFAULT_USER_NAME}_{i}",
                Email = DEFAULT_EMAIL
            };

            string password = "!Ab12345";

            var userCreatedResult = await _sut.CreateUserAsync(userMock, password);
        }

        // Act

        var listUsersResult = await _sut.ListUsersAsync(string.Empty);

        // Assert

        Assert.NotNull(listUsersResult);
        Assert.Equal(count, listUsersResult.Count);
    }

    #region Utils

    private DatabaseContext GetDatabaseContext()
    {
        return _contextWrapper.ProvideDatabaseContext();
    }

    #endregion Utils
}
