using Domain.Entities.Identity;
using Domain.Repositories;
using Infrastructure.IntegrationTests.Common.Fixtures;
using Infrastructure.Repositories.Identity;
using Infrastructure.Services.DatabaseContextWrapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.IntegrationTests.Database;

public class IdentityTests : IClassFixture<DatabaseFixture>
{
    private readonly IIdentityRepository _repository;

    public IdentityTests(DatabaseFixture fixture)
    {
        var dbContextWrapper = fixture.ServiceProvider.GetRequiredService<IDatabaseContextWrapper>();
        var userManager = fixture.ServiceProvider.GetRequiredService<UserManager<User>>();
        _repository = new IdentityRepository(dbContextWrapper, userManager);
    }

    [Fact]
    public async Task CreateUser_CreateAnUser_ReturnsTheCreatedUser()
    {
        // Arrange

        string userName = "Claudiu";
        string email = "claudiu@item-trading.app";
        string password = "!Abcd12345";

        var user = new User
        {
            Id = User.GenerateId(),
            UserName = userName,
            Email = email
        };

        // Act

        var creationResponse = await _repository.CreateUserAsync(user, password);
        var getUserResponse = await _repository.GetUserByIdAsync(user.Id);
        var checkPasswordResponse = await _repository.CheckPasswordAsync(user, password);

        // Assert

        Assert.NotNull(creationResponse);
        Assert.True(creationResponse.Succeeded);
        Assert.NotNull(getUserResponse);
        Assert.Equal(user.Id, getUserResponse.Id);
        Assert.Equal(userName, getUserResponse.UserName);
        Assert.Equal(email, getUserResponse.Email);
        Assert.Equal(userName, getUserResponse.UserName);
        Assert.True(checkPasswordResponse);
    }

    [Fact]
    public async Task CreateUser_CreateAnUserThenUpdateIt_ReturnsTheUpdatedUser()
    {
        // Arrange

        string userName = "Claudiu0";
        string email = "claudiu0@item-trading.app";
        string password = "!Abcd12345";
        int initialCashAmount = 10;
        int updatedCashAmount = 100;

        var user = new User
        {
            Id = User.GenerateId(),
            UserName = userName,
            Email = email
        };

        user.UpdateCashAmount(initialCashAmount);

        // Act

        var creationResponse = await _repository.CreateUserAsync(user, password);
        user.UpdateCashAmount(updatedCashAmount);
        var updateUserResponse = await _repository.UpdateUserAsync(user);
        var getUserResponse = await _repository.GetUserByIdAsync(user.Id);

        // Assert

        Assert.NotNull(creationResponse);
        Assert.True(creationResponse.Succeeded);
        Assert.NotNull(getUserResponse);
        Assert.Equal(user.Id, getUserResponse.Id);
        Assert.Equal(userName, getUserResponse.UserName);
        Assert.Equal(email, getUserResponse.Email);
        Assert.Equal(userName, getUserResponse.UserName);
        Assert.Equal(updatedCashAmount, getUserResponse.Cash);
    }

    [Fact]
    public async Task GetUserById_CreateAnUserAndGetUserById_ReturnsTheCreatedUserById()
    {
        // Arrange

        string userName = "Claudiu1";
        string email = "claudiu1@item-trading.app";
        string password = "!Abcd12345";

        var user = new User
        {
            Id = User.GenerateId(),
            UserName = userName,
            Email = email
        };

        // Act

        var creationResponse = await _repository.CreateUserAsync(user, password);
        var getUserById = await _repository.GetUserByIdAsync(user.Id);

        // Assert

        Assert.NotNull(creationResponse);
        Assert.True(creationResponse.Succeeded);
        Assert.NotNull(getUserById);
        Assert.Equal(user.Id, getUserById.Id);
        Assert.Equal(userName, getUserById.UserName);
        Assert.Equal(email, getUserById.Email);
    }

    [Fact]
    public async Task GetUserByName_CreateAnUserAndGetUserByName_ReturnsTheCreatedUserByName()
    {
        // Arrange

        string userName = "Claudiu2";
        string email = "claudiu2@item-trading.app";
        string password = "!Abcd12345";

        var user = new User
        {
            Id = User.GenerateId(),
            UserName = userName,
            Email = email
        };

        // Act

        var creationResponse = await _repository.CreateUserAsync(user, password);
        var getUserByName = await _repository.GetUserByNameAsync(userName);

        // Assert

        Assert.NotNull(creationResponse);
        Assert.True(creationResponse.Succeeded);
        Assert.NotNull(getUserByName);
        Assert.Equal(user.Id, getUserByName.Id);
        Assert.Equal(userName, getUserByName.UserName);
        Assert.Equal(email, getUserByName.Email);
    }

    [Fact]
    public async Task GetUsername_CreateAnUserAndGetUsernameById_ReturnsTheCreatedUsersName()
    {
        // Arrange

        string userName = "Claudiu3";
        string email = "claudiu3@item-trading.app";
        string password = "!Abcd12345";

        var user = new User
        {
            Id = User.GenerateId(),
            UserName = userName,
            Email = email
        };

        // Act

        var creationResponse = await _repository.CreateUserAsync(user, password);
        var getUsername = await _repository.GetUsernameAsync(user.Id);

        // Assert

        Assert.NotNull(creationResponse);
        Assert.True(creationResponse.Succeeded);
        Assert.NotNull(getUsername);
        Assert.Equal(userName, getUsername);
    }

    [Fact]
    public async Task GetClaims_CreateAnUserAndGetUserClaims_ReturnsTheUserClaims()
    {
        // Arrange

        string userName = "Claudiu4";
        string email = "claudiu4@item-trading.app";
        string password = "!Abcd12345";

        var user = new User
        {
            Id = User.GenerateId(),
            UserName = userName,
            Email = email
        };

        // Act

        var creationResponse = await _repository.CreateUserAsync(user, password);
        var getUserClaims = await _repository.GetClaimsAsync(user);

        // Assert

        Assert.NotNull(creationResponse);
        Assert.True(creationResponse.Succeeded);
        Assert.NotNull(getUserClaims);
    }

    [Fact]
    public async Task ListUsers_CreateSeveralUsersThenListThem_ReturnsCreatedUsersInAList()
    {
        // Arrange
        var dbFixture = await DatabaseFixture.BuildDatabaseFixture();
        var dbContextWrapper = dbFixture.ServiceProvider.GetRequiredService<IDatabaseContextWrapper>();
        var userManager = dbFixture.ServiceProvider.GetRequiredService<UserManager<User>>();
        var repository = new IdentityRepository(dbContextWrapper, userManager);

        int expectedUsersCount = 5;

        string baseUserName = "User_{0}";
        string baseUserEmail = "User_{0}@email.com";
        string password = "!Abcd12345";
        bool usersCreationSucceeded = true;

        var usersToBeCreated = new User[expectedUsersCount];

        for (int i = 0; i < expectedUsersCount; i++)
        {
            var newUser = new User
            {
                Id = User.GenerateId(),
                UserName = string.Format(baseUserName, i),
                Email = string.Format(baseUserEmail, i)
            };

            usersToBeCreated[i] = newUser;
        }

        // Act

        for (int i = 0; i < expectedUsersCount; i++)
        {
            var createUserResponse = await repository.CreateUserAsync(usersToBeCreated[i], password);

            if (!createUserResponse.Succeeded)
            {
                usersCreationSucceeded = false;
                break;
            }
        }

        List<string> foundUsers = null!;

        if (usersCreationSucceeded)
        {
            foundUsers = await repository.ListUsersAsync("");
        }

        // Assert

        Assert.True(usersCreationSucceeded);
        Assert.NotNull(foundUsers);
        Assert.Equal(expectedUsersCount, foundUsers.Count);
        Assert.All(usersToBeCreated, x => foundUsers.Any(y => y == x.Id));
    }
}
