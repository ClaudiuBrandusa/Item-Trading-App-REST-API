using Domain.Entities.Identity;
using Domain.Repositories;
using Infrastructure.IntegrationTests.Common;
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
}
