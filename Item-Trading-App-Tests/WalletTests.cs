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

    [Fact(DisplayName = "Get wallet")]
    public async Task GetWallet()
    {
        // Arrange

        var userStub = new User
        {
            Id = userId,
            Cash = defaultCashValue,
            UserName = "username"
        };

        await _userManager.CreateAsync(userStub);

        var queryStub = new GetUserWalletQuery { UserId = userId };

        // Act

        var result = await _sut.GetWalletAsync(queryStub);

        // Assert

        Assert.True(result.Success, "The result has to be successful");
        Assert.Equal(userId, result.UserId);
        Assert.Equal(defaultCashValue, result.Cash);
    }

    [Fact(DisplayName = "Get wallet without creating an user first")]
    public async Task GetWalletWithoutCreatingAnUserFirst()
    {
        // Arrange

        var queryStub = new GetUserWalletQuery { UserId = userId };

        // Act

        var result = await _sut.GetWalletAsync(queryStub);

        // Assert

        Assert.False(result.Success, "The result should be unsuccessful because no user was created first");
    }

    [Fact(DisplayName = "Update wallet")]
    public async Task UpdateWallet()
    {
        // Arrange

        var userStub = new User
        {
            Id = userId,
            Cash = defaultCashValue,
            UserName = "username"
        };

        await _userManager.CreateAsync(userStub);

        int newCashAmount = defaultCashValue + 100;

        var commandStub = new UpdateWalletCommand
        {
            UserId = userId,
            Quantity = newCashAmount,
        };

        // Act

        var result = await _sut.UpdateWalletAsync(commandStub);

        // Assert

        Assert.True(result.Success, "The result has to be successful");
        Assert.Equal(userId, result.UserId);
        Assert.Equal(newCashAmount, result.Cash);
    }

    [Fact(DisplayName = "Update wallet without creating an user first")]
    public async Task UpdateWalletWithoutCreatingAnUserFirst()
    {
        // Arrange

        int newCashAmount = defaultCashValue + 100;

        var commandStub = new UpdateWalletCommand
        {
            UserId = userId,
            Quantity = newCashAmount,
        };

        // Act

        var result = await _sut.UpdateWalletAsync(commandStub);

        // Assert

        Assert.False(result.Success, "The result should be unsuccessful because no user was created first");
    }

    [Fact(DisplayName = "Get user cash amount")]
    public async Task GetUserCashAmount()
    {
        // Arrange

        var userStub = new User
        {
            Id = userId,
            Cash = defaultCashValue,
            UserName = "username"
        };

        await _userManager.CreateAsync(userStub);

        var queryStub = new GetUserCashQuery { UserId = userId };

        // Act

        var userCashAmount = await _sut.GetUserCashAsync(queryStub);

        // Assert

        Assert.True(userCashAmount == defaultCashValue, "The user cash amount has to be equal to the cash amount that was added");
    }

    [Fact(DisplayName = "Get user cash amount without creating an user first")]
    public async Task GetUserCashAmountWithoutCreatingAnUserFirst()
    {
        // Arrange

        var queryStub = new GetUserCashQuery { UserId = userId };

        // Act

        var userCashAmount = await _sut.GetUserCashAsync(queryStub);

        // Assert

        Assert.True(0 == userCashAmount, "The cash amount should be 0 because no user was added before");
    }

    [Fact(DisplayName = "Take cash amount from user")]
    public async Task TakeCashFromUser()
    {
        // Arrange

        var userStub = new User
        {
            Id = userId,
            Cash = defaultCashValue,
            UserName = "username"
        };

        await _userManager.CreateAsync(userStub);

        int takenAmount = 100;

        var commandStub = new TakeCashCommand
        {
            UserId = userId,
            Amount = takenAmount
        };

        // Act

        var result = await _sut.TakeCashAsync(commandStub);

        // Assert

        Assert.True(result, "The result must be true");
    }

    [Fact(DisplayName = "Take more cash than the user has")]
    public async Task TakeMoreCashThanTheUserHas()
    {
        // Arrange

        var userStub = new User
        {
            Id = userId,
            Cash = defaultCashValue,
            UserName = "username"
        };

        await _userManager.CreateAsync(userStub);

        int takenAmount = defaultCashValue + 100;

        var commandStub = new TakeCashCommand
        {
            UserId = userId,
            Amount = takenAmount
        };

        // Act

        var result = await _sut.TakeCashAsync(commandStub);

        // Assert

        Assert.False(result, "The result should be false because the user should not have that amount of cash");
    }

    [Fact(DisplayName = "Take cash amount from user without creating an user first")]
    public async Task TakeCashFromUserWithoutCreatingAnUserFirst()
    {
        // Arrange

        int takenAmount = 100;

        var commandStub = new TakeCashCommand
        {
            UserId = userId,
            Amount = takenAmount
        };

        // Act

        var result = await _sut.TakeCashAsync(commandStub);

        // Assert

        Assert.False(result, "The result should be false because no user was created first");
    }

    [Theory(DisplayName = "Give cash amount to user")]
    [InlineData(100)]
    [InlineData(200)]
    [InlineData(10)]
    public async Task GiveCashToUser(int givenAmount)
    {
        // Arrange

        var userStub = new User
        {
            Id = userId,
            Cash = defaultCashValue,
            UserName = "username"
        };

        await _userManager.CreateAsync(userStub);

        var commandStub = new GiveCashCommand
        {
            UserId = userId,
            Amount = givenAmount
        };

        // Act

        var giveCashResult = await _sut.GiveCashAsync(commandStub);

        var result = await _sut.GetUserCashAsync(new GetUserCashQuery { UserId = userId });

        // Assert

        Assert.True(giveCashResult, "The result has to be true");
        Assert.Equal(defaultCashValue + givenAmount, result);
    }

    [Theory(DisplayName = "Give invalid cash amount to user")]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task GiveInvalidCashToUser(int givenAmount)
    {
        // Arrange

        var userStub = new User
        {
            Id = userId,
            Cash = defaultCashValue,
            UserName = "username"
        };

        await _userManager.CreateAsync(userStub);

        var commandStub = new GiveCashCommand
        {
            UserId = userId,
            Amount = givenAmount
        };

        // Act

        var giveCashResult = await _sut.GiveCashAsync(commandStub);

        var result = await _sut.GetUserCashAsync(new GetUserCashQuery { UserId = userId });

        // Assert

        Assert.False(giveCashResult, "The result has to be false because an invalid amount was given");
        Assert.Equal(defaultCashValue, result);
    }

    [Fact(DisplayName = "Give cash amount to user without creating the user before")]
    public async Task GiveCashToUserWithoutCreatingTheUserBefore()
    {
        // Arrange

        var commandStub = new GiveCashCommand
        {
            UserId = userId,
            Amount = defaultCashValue
        };

        // Act

        var giveCashResult = await _sut.GiveCashAsync(commandStub);

        // Assert

        Assert.False(giveCashResult, "The result has to be false because no user was created before");
    }
}
