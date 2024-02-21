using Item_Trading_App_REST_API.Resources.Commands.Wallet;
using Item_Trading_App_REST_API.Entities;
using Item_Trading_App_REST_API.Resources.Queries.Wallet;
using Item_Trading_App_REST_API.Services.Wallet;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Item_Trading_App_Tests;

public class WalletTests
{
    private readonly IWalletService _sut; // service under test
    private readonly UserManager<User> _userManager;
    private readonly string userId = Guid.NewGuid().ToString();
    private readonly int defaultCashValue = 150;

    public WalletTests()
    {
        var dbContext = TestingUtils.GetDatabaseContext();

        _userManager = TestingUtils.GetUserManager(new UserStore<User>(dbContext));

        _sut = new WalletService(_userManager);
    }

    [Theory(DisplayName = "Get wallet")]
    [InlineData(true)]
    [InlineData(false)]
    public async void GetWallet(bool createUser)
    {
        // Arrange

        if (createUser)
        {
            await _userManager.CreateAsync(new User
            {
                Id = userId,
                Cash = defaultCashValue,
                UserName = "username"
            });
        }

        var queryStub = new GetUserWalletQuery { UserId = userId };

        // Act

        var result = await _sut.GetWalletAsync(queryStub);

        // Assert

        if (createUser)
        {
            Assert.True(result.Success, "The result has to be successful");
            Assert.Equal(userId, result.UserId);
            Assert.Equal(defaultCashValue, result.Cash);
        }
        else
        {
            Assert.False(result.Success, "The result should be unsuccessful");
        }
    }

    [Theory(DisplayName = "Update wallet")]
    [InlineData(true)]
    [InlineData(false)]
    public async void UpdateWallet(bool createUser)
    {
        // Arrange

        if (createUser)
        {
            await _userManager.CreateAsync(new User
            {
                Id = userId,
                Cash = defaultCashValue,
                UserName = "username"
            });
        }

        int newCashAmount = defaultCashValue + 100;

        var commandStub = new UpdateWalletCommand
        {
            UserId = userId,
            Quantity = newCashAmount,
        };

        // Act

        var result = await _sut.UpdateWalletAsync(commandStub);

        // Assert

        if (createUser)
        {
            Assert.True(result.Success, "The result has to be successful");
            Assert.Equal(userId, result.UserId);
            Assert.Equal(newCashAmount, result.Cash);
        }
        else
        {
            Assert.False(result.Success, "The result should be unsuccessful");
        }
    }

    [Theory(DisplayName = "Get user cash amount")]
    [InlineData(true)]
    [InlineData(false)]
    public async void GetUserCashAmount(bool createUser)
    {
        // Arrange

        if (createUser)
        {
            await _userManager.CreateAsync(new User
            {
                Id = userId,
                Cash = defaultCashValue,
                UserName = "username"
            });
        }

        var queryStub = new GetUserCashQuery { UserId = userId };

        // Act

        var userCashAmount = await _sut.GetUserCashAsync(queryStub);

        // Assert

        if (createUser)
            Assert.True(userCashAmount == defaultCashValue);
        else
            Assert.Equal(0, userCashAmount);
    }

    [Theory(DisplayName = "Take cash amount from user")]
    [InlineData(true, 100)]
    [InlineData(true, 200)]
    [InlineData(true, 10)]
    [InlineData(false, 50)]
    public async void TakeCashFromUser(bool createUser, int takenAmount)
    {
        // Arrange

        if (createUser)
        {
            await _userManager.CreateAsync(new User
            {
                Id = userId,
                Cash = defaultCashValue,
                UserName = "username"
            });
        }

        var commandStub = new TakeCashCommand
        {
            UserId = userId,
            Amount = takenAmount
        };

        // Act

        var result = await _sut.TakeCashAsync(commandStub);

        // Assert

        if (createUser && takenAmount <= defaultCashValue)
            Assert.True(result);
        else
            Assert.False(result);
    }

    [Theory(DisplayName = "Give cash amount to user")]
    [InlineData(true, 100)]
    [InlineData(true, 200)]
    [InlineData(true, 10)]
    [InlineData(false, 50)]
    public async void GiveCashToUser(bool createUser, int givenAmount)
    {
        // Arrange

        if (createUser)
        {
            await _userManager.CreateAsync(new User
            {
                Id = userId,
                Cash = defaultCashValue,
                UserName = "username"
            });
        }

        var commandStub = new GiveCashCommand
        {
            UserId = userId,
            Amount = givenAmount
        };

        // Act

        var giveCashResult = await _sut.GiveCashAsync(commandStub);

        // Assert

        if (createUser)
        {
            Assert.True(giveCashResult);
            var result = await _sut.GetUserCashAsync(new GetUserCashQuery { UserId = userId });
            Assert.Equal(defaultCashValue + givenAmount, result);
        }
        else
        {
            Assert.False(giveCashResult);
        }
    }
}
