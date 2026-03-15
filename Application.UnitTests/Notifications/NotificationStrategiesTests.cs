using Application.Services.ConnectedUsers;
using Application.Utils.Notifications.Strategies;
using Item_Trading_App_Contracts.Notifications;
using Item_Trading_App_Contracts.Notifications.Content;

namespace Application_UnitTests.Notifications;

public class NotificationStrategiesTests
{
    [Fact]
    public async Task NotifySingleUserStrategy_ShouldCallTheCorrectMethodFromTheRepository()
    {
        // Arrange
        const string expectedUserId = "userId";
        var sut = new NotifySingleUserStrategy(expectedUserId);
        var notificationMock = new Notification<MessageContent>()
        {
            Type = "type",
            Content = new MessageContent()
            {
                Content = "content",
                CreatedDateTime = DateTime.UtcNow
            }
        };
        var connectedUsersRepositoryMock = new Mock<IConnectedUsersRepository>();

        // Act
        await sut.Notify(notificationMock, connectedUsersRepositoryMock.Object);

        // Assert
        connectedUsersRepositoryMock.Verify(x => x.NotifyUserAsync(expectedUserId, notificationMock), Times.Once());
    }

    [Fact]
    public async Task NotifyMultipleUsersStrategy_ShouldCallTheCorrectMethodFromTheRepository()
    {
        // Arrange
        const string expectedUserId = "userId";
        const string secondUserId = "secondUserId";
        var userIdsToBeNotified = new[] { expectedUserId, secondUserId };
        var sut = new NotifyMultipleUsersStrategy(userIdsToBeNotified);
        var notificationMock = new Notification<MessageContent>()
        {
            Type = "type",
            Content = new MessageContent()
            {
                Content = "content",
                CreatedDateTime = DateTime.UtcNow
            }
        };
        var connectedUsersRepositoryMock = new Mock<IConnectedUsersRepository>();

        // Act
        await sut.Notify(notificationMock, connectedUsersRepositoryMock.Object);

        // Assert
        connectedUsersRepositoryMock.Verify(x => x.NotifyUsersAsync(userIdsToBeNotified, notificationMock), Times.Once());
    }

    [Fact]
    public async Task NotifyAllUsersStrategy_ShouldCallTheCorrectMethodFromTheRepository()
    {
        // Arrange
        const string expectedUserId = "userId";
        var sut = new NotifyAllUsersStrategy();
        var notificationMock = new Notification<MessageContent>()
        {
            Type = "type",
            Content = new MessageContent()
            {
                Content = "content",
                CreatedDateTime = DateTime.UtcNow
            }
        };
        var connectedUsersRepositoryMock = new Mock<IConnectedUsersRepository>();

        // Act
        await sut.Notify(notificationMock, connectedUsersRepositoryMock.Object);

        // Assert
        connectedUsersRepositoryMock.Verify(x => x.NotifyUsersAsync(notificationMock), Times.Once());
    }

    [Fact]
    public async Task NotifyAllUsersExceptStrategy_ShouldCallTheCorrectMethodFromTheRepository()
    {
        // Arrange
        const string expectedUserId = "userId";
        const string exceptedUserId = "anotherUser";
        var availableUserIds = new string[] { expectedUserId, exceptedUserId };
        var sut = new NotifyAllUsersExceptStrategy(exceptedUserId);
        var notificationMock = new Notification<MessageContent>()
        {
            Type = "type",
            Content = new MessageContent()
            {
                Content = "content",
                CreatedDateTime = DateTime.UtcNow
            }
        };
        var connectedUsersRepositoryMock = new Mock<IConnectedUsersRepository>();
        connectedUsersRepositoryMock.Setup(x => x.ListUserIds())
            .Returns(availableUserIds);

        // Act
        await sut.Notify(notificationMock, connectedUsersRepositoryMock.Object);

        // Assert
        connectedUsersRepositoryMock.Verify(x => x.NotifyUsersAsync(new string[] { expectedUserId }, notificationMock), Times.Once());
    }
}
