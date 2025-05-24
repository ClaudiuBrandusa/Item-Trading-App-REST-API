using Application.Services.ConnectedUsers;
using Application.Services.Notification;
using Application.Utils.Notifications.NotificationStrategies;
using Domain.Entities.Identity;

namespace Application_UnitTests.Notifications;

public class NotifyUserStrategyTests
{
    private readonly IHubClientsWrapper hubClientsWrapper;
    private readonly IConnectedUsersRepository connectedUsersRepository;
    private HashSet<string> notifiedUsers = new();
    private bool notificationCallReceivedForAllUsers = false;
    private readonly string[] defaultActiveUserIds;

    public NotifyUserStrategyTests()
    {
        defaultActiveUserIds = new string[]
        {
            GenerateUserId(),
            GenerateUserId(),
            GenerateUserId()
        };

        var hubClientsWrapperMock = new Mock<IHubClientsWrapper>();

        hubClientsWrapperMock.Setup(x => x.SendTo(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
            .Returns((string target, string message, object content) =>
            {
                notifiedUsers.Add(target);

                return Task.CompletedTask;
            });

        hubClientsWrapperMock.Setup(x => x.SendTo(It.IsAny<string[]>(), It.IsAny<string>(), It.IsAny<object>()))
            .Returns((string[] targets, string message, object content) =>
            {
                if (content.ToString() == "All")
                {
                    notificationCallReceivedForAllUsers = true;
                    return Task.CompletedTask;
                }

                targets.All(target => notifiedUsers.Add(target));

                return Task.CompletedTask;
            });

        hubClientsWrapper = hubClientsWrapperMock.Object;
        var connectedUsersRepositoryMock = new Mock<IConnectedUsersRepository>();

        connectedUsersRepositoryMock.Setup(x => x.UserExist(It.IsAny<string>()))
            .Returns((string userId) => true); // let's assume that any user id is valid and it exist

        connectedUsersRepositoryMock.Setup(x => x.GetActiveUserIds())
            .Returns(() => defaultActiveUserIds);

        connectedUsersRepository = connectedUsersRepositoryMock.Object;
    }

    [Fact(DisplayName="Create an instance of single user notification strategy and notify a given user")]
    public async Task NotifySingleUserStrategy_CreateAnInstanceOfThisStrategyThenNotify_NotifiesTheUser()
    {
        // Arrange

        string targetUserId = GenerateUserId();
        INotifyUserStrategy strategy = new NotifySingleUserStrategy(targetUserId);
        var notification = new { };

        // Act

        await strategy.Notify(hubClientsWrapper, connectedUsersRepository, notification);

        // Assert

        Assert.True(WasUserNotified(targetUserId), "The target user must be notified");
    }

    [Fact(DisplayName = "Create an instance of multiple users notification strategy and notify a given amount of users")]
    public async Task NotifyMultipleUsersStrategy_CreateAnInstanceOfThisStrategyThenNotify_NotifiesTheUsers()
    {
        // Arrange

        const int expectedAmountOfUsers = 3;
        var userIds = GenerateUserIds(expectedAmountOfUsers);

        INotifyUserStrategy strategy = new NotifyMultipleUsersStrategy(userIds);
        object notification = "";

        // Act

        await strategy.Notify(hubClientsWrapper, connectedUsersRepository, notification);

        // Assert

        Assert.All(userIds, targetUserId => WasUserNotified(targetUserId));
    }

    [Fact(DisplayName = "Create an instance of all users notification strategy and notify all of the connected users")]
    public async Task NotifyAllUsersStrategy_CreateAnInstanceOfThisStrategyThenNotify_NotifiesAllTheUsers()
    {
        // Arrange

        INotifyUserStrategy strategy = new NotifyAllUsersStrategy();
        object notification = "All";

        // Act

        await strategy.Notify(hubClientsWrapper, connectedUsersRepository, notification);

        // Assert

        Assert.True(notificationCallReceivedForAllUsers, "This flag must be true, otherwise, not all of the users were notified.");
    }

    [Fact(DisplayName = "Create an instance of all users except a user notification strategy and notifty all the users but a given user")]
    public async Task NotifyAllUsersExceptStrategy_CreateAnInstanceOfThisStrategyThenNotify_NotifiesAllTheUsersButOne()
    {
        // Arrange

        string exceptedUserId = GenerateUserId();

        INotifyUserStrategy strategy = new NotifyAllUsersExceptStrategy(exceptedUserId);
        var notification = new { };

        // Act

        await strategy.Notify(hubClientsWrapper, connectedUsersRepository, notification);

        // Assert

        Assert.False(WasUserNotified(exceptedUserId), "The excepted user shouldn't be notified.");
    }

    #region Utils

    private bool WasUserNotified(string userId)
    {
        return notifiedUsers.TryGetValue(userId, out _);
    }

    private string GenerateUserId()
    {
        return User.GenerateId();
    }

    private string[] GenerateUserIds(int amount)
    {
        var arr = new string[amount];

        for (int i = 0; i < amount; i++)
            arr[i] = GenerateUserId();

        return arr;
    }

    #endregion Utils
}
