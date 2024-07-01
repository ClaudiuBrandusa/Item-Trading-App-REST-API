using Application.Services.Wallet;
using Domain.Identity;
using Application.Behaviors.Wallet.GetWallet;
using Application.Behaviors.Wallet.GiveCash;
using Application.Behaviors.Wallet.GetCash;
using Application.Behaviors.Wallet.TakeCash;
using Application.Behaviors.Wallet.UpdateWallet;
using Domain.Repositories;

namespace Application_UnitTests;

public class WalletTests
{
    private readonly IWalletService _sut; // service under test
    private readonly string userId = Guid.NewGuid().ToString();
    private readonly int defaultCashValue = 150;
    private List<User> collection;

    public WalletTests()
    {
        collection = new List<User>();

        var userRepositoryMock = TestingUtils.CreateRepositoryMock<User, IUserRepository>(collection);

        #region MediatorMocks

        userRepositoryMock.Setup(repo => repo.GetUserByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string userId) =>
            {
                return GetUserById(userId);
            });

        userRepositoryMock.Setup(repo => repo.UpdateUserAsync(It.IsAny<User>()))
            .ReturnsAsync((User user) =>
            {
                var entity = GetUserById(user.Id);

                if (entity is null)
                    return false;

                int index = collection.IndexOf(entity);

                collection[index] = user;

                return true;
            });

        #endregion MediatorMocks

        _sut = new WalletService(userRepositoryMock.Object);
    }

    [Fact(DisplayName = "Get wallet")]
    public async Task GetWallet_CreateUserThenGetWallet_ReturnsUsersWallet()
    {
        // Arrange

        var userStub = new User
        {
            Id = userId,
            Cash = defaultCashValue,
            UserName = "username"
        };

        CreateUser(userStub);

        var queryStub = new GetUserWalletQuery { UserId = userId };

        // Act

        var result = await _sut.GetWalletAsync(queryStub);

        // Assert

        Assert.True(result.Success, "The result has to be successful");
        Assert.Equal(userId, result.UserId);
        Assert.Equal(defaultCashValue, result.Cash);
    }

    [Fact(DisplayName = "Get wallet without creating an user first")]
    public async Task GetWallet_GetWalletWithoutCreatingAnUserFirst_ShouldFail()
    {
        // Arrange

        var queryStub = new GetUserWalletQuery { UserId = userId };

        // Act

        var result = await _sut.GetWalletAsync(queryStub);

        // Assert

        Assert.False(result.Success, "The result should be unsuccessful because no user was created first");
    }

    [Fact(DisplayName = "Update wallet")]
    public async Task UpdateWallet_CreateUserThenUpdateWallet_ReturnsUpdatedWallet()
    {
        // Arrange

        var userStub = new User
        {
            Id = userId,
            Cash = defaultCashValue,
            UserName = "username"
        };

        CreateUser(userStub);

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
    public async Task UpdateWallet_UpdateWalletWithoutCreatingAnUserFirst_ShouldFail()
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
    public async Task GetUserCash_CreateUserThenGetUserCashAmount_ReturnsUsersCashAmount()
    {
        // Arrange

        var userStub = new User
        {
            Id = userId,
            Cash = defaultCashValue,
            UserName = "username"
        };

        CreateUser(userStub);

        var queryStub = new GetUserCashQuery { UserId = userId };

        // Act

        var userCashAmount = await _sut.GetUserCashAsync(queryStub);

        // Assert

        Assert.True(userCashAmount == defaultCashValue, "The user cash amount has to be equal to the cash amount that was added");
    }

    [Fact(DisplayName = "Get user cash amount without creating an user first")]
    public async Task GetUserCash_GetUserCashAmountWithoutCreatingAnUserFirst_Returns0()
    {
        // Arrange

        var queryStub = new GetUserCashQuery { UserId = userId };

        // Act

        var userCashAmount = await _sut.GetUserCashAsync(queryStub);

        // Assert

        Assert.True(0 == userCashAmount, "The cash amount should be 0 because no user was added before");
    }

    [Fact(DisplayName = "Take cash amount from user")]
    public async Task TakeCash_CreateUserThenTakeCashFromUser_ReturnsTrue()
    {
        // Arrange

        var userStub = new User
        {
            Id = userId,
            Cash = defaultCashValue,
            UserName = "username"
        };

        CreateUser(userStub);

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
    public async Task TakeCash_CreateUserThenTakeMoreCashThanTheUserHas_ReturnsFalse()
    {
        // Arrange

        var userStub = new User
        {
            Id = userId,
            Cash = defaultCashValue,
            UserName = "username"
        };

        CreateUser(userStub);

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
    public async Task TakeCash_TakeCashFromUserWithoutCreatingAnUserFirst_ReturnsFalse()
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
    public async Task GiveCash_CreateUserThenGiveCash_ReturnsTrue(int givenAmount)
    {
        // Arrange

        var userStub = new User
        {
            Id = userId,
            Cash = defaultCashValue,
            UserName = "username"
        };

        CreateUser(userStub);

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
    public async Task GiveCash_CreateUserThenGiveInvalidCashToUser_ReturnsFalse(int givenAmount)
    {
        // Arrange

        var userStub = new User
        {
            Id = userId,
            Cash = defaultCashValue,
            UserName = "username"
        };

        CreateUser(userStub);

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
    public async Task GiveCash_GiveCashToUserWithoutCreatingTheUserBefore_ReturnsFalse()
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

    #region Utils

    private void CreateUser(User user)
    {
        collection.Add(user);
    }

    private User? GetUserById(string userId)
    {
        return collection.FirstOrDefault(x => x.Id == userId);
    }

    #endregion Utils
}
