using Application.Services.ConnectedUsers;
using Application.Utils.Notifications;
using Application_UnitTests.Notifications.Helpers;
using Infrastructure.Services.Notification;
using Item_Trading_App_Contracts.Notifications;
using Item_Trading_App_Contracts.Notifications.Content;

namespace Application_UnitTests.Notifications;

public class ClientNotificationServiceTests
{
    [Fact]
    public async Task SendCreatedNotification_WithoutCustomData()
    {
        var mock = new Mock<IConnectedUsersRepository>();

        var repo = mock.Object;

        var sut = new ClientNotificationService(repo);

        var nusMock = new Mock<INotifyUserStrategy>();

        var nus = nusMock.Object;
        var expectedCategoryType = string.Empty;
        var expectedNotificationType = "data_created";
        var expectedId = string.Empty;

        await sut.SendCreatedNotificationAsync(nus, expectedCategoryType, expectedId);

        var validator = SetValidator()
            .HasNotificationType(expectedNotificationType)
            .HasContent(expectedCategoryType, expectedId);

        nusMock.Verify(x => x.Notify(It.Is<Notification<ModifiedContentWithCustomData>>((notification) => validator.Validate(notification)), repo), Times.Once);
    }

    [Fact]
    public async Task SendCreatedNotification_WithCustomData()
    {
        var mock = new Mock<IConnectedUsersRepository>();

        var repo = mock.Object;

        var sut = new ClientNotificationService(repo);

        var nusMock = new Mock<INotifyUserStrategy>();

        var nus = nusMock.Object;
        var expectedCategoryType = string.Empty;
        var expectedNotificationType = "data_created";
        var expectedId = string.Empty;
        var expectedCustomData = new { };

        await sut.SendCreatedNotificationAsync(nus, expectedCategoryType, expectedId, expectedCustomData);

        var validator = SetValidator()
            .HasNotificationType(expectedNotificationType)
            .HasContent(expectedCategoryType, expectedId, expectedCustomData);

        nusMock.Verify(x => x.Notify(It.Is<Notification<ModifiedContentWithCustomData>>((notification) => validator.Validate(notification)), repo), Times.Once);
    }


    [Fact]
    public async Task SendMessageNotification()
    {
        var mock = new Mock<IConnectedUsersRepository>();

        var repo = mock.Object;

        var sut = new ClientNotificationService(repo);

        var nusMock = new Mock<INotifyUserStrategy>();

        var nus = nusMock.Object;
        var expectedCategoryType = string.Empty;
        var expectedDateTime = DateTime.UtcNow;
        var expectedNotificationType = "information";
        var expectedId = string.Empty;

        await sut.SendMessageNotificationAsync(nus, expectedCategoryType, expectedDateTime);

        var validator = SetValidator()
            .HasNotificationType(expectedNotificationType)
            .HasMessageContent(expectedCategoryType, expectedDateTime);

        nusMock.Verify(x => x.Notify(It.Is<Notification<MessageContent>>((notification) => validator.Validate(notification)), repo), Times.Once);
    }

    [Fact]
    public async Task SendUpdatedNotification_WithoutCustomData()
    {
        var mock = new Mock<IConnectedUsersRepository>();

        var repo = mock.Object;

        var sut = new ClientNotificationService(repo);

        var nusMock = new Mock<INotifyUserStrategy>();

        var nus = nusMock.Object;
        var expectedCategoryType = string.Empty;
        var expectedNotificationType = "data_changed";
        var expectedId = string.Empty;

        await sut.SendUpdatedNotificationAsync(nus, expectedCategoryType, expectedId);

        var validator = SetValidator()
            .HasNotificationType(expectedNotificationType)
            .HasContent(expectedCategoryType, expectedId);

        nusMock.Verify(x => x.Notify(It.Is<Notification<ModifiedContentWithCustomData>>((notification) => validator.Validate(notification)), repo), Times.Once);
    }

    [Fact]
    public async Task SendUpdatedNotification_WithCustomData()
    {
        var mock = new Mock<IConnectedUsersRepository>();

        var repo = mock.Object;

        var sut = new ClientNotificationService(repo);

        var nusMock = new Mock<INotifyUserStrategy>();

        var nus = nusMock.Object;
        var expectedCategoryType = string.Empty;
        var expectedNotificationType = "data_changed";
        var expectedId = string.Empty;
        var expectedCustomData = new { };

        await sut.SendUpdatedNotificationAsync(nus, expectedCategoryType, expectedId, expectedCustomData);

        var validator = SetValidator()
            .HasNotificationType(expectedNotificationType)
            .HasContent(expectedCategoryType, expectedId, expectedCustomData);

        nusMock.Verify(x => x.Notify(It.Is<Notification<ModifiedContentWithCustomData>>((notification) => validator.Validate(notification)), repo), Times.Once);
    }

    [Fact]
    public async Task SendDeletedNotification_WithoutCustomData()
    {
        var mock = new Mock<IConnectedUsersRepository>();

        var repo = mock.Object;

        var sut = new ClientNotificationService(repo);

        var nusMock = new Mock<INotifyUserStrategy>();

        var nus = nusMock.Object;
        var expectedCategoryType = string.Empty;
        var expectedNotificationType = "data_deleted";
        var expectedId = string.Empty;

        await sut.SendDeletedNotificationAsync(nus, expectedCategoryType, expectedId);

        var validator = SetValidator()
            .HasNotificationType(expectedNotificationType)
            .HasContent(expectedCategoryType, expectedId);

        nusMock.Verify(x => x.Notify(It.Is<Notification<ModifiedContentWithCustomData>>((notification) => validator.Validate(notification)), repo), Times.Once);
    }

    [Fact]
    public async Task SendDeletedNotification_WithCustomData()
    {
        var mock = new Mock<IConnectedUsersRepository>();

        var repo = mock.Object;

        var sut = new ClientNotificationService(repo);

        var nusMock = new Mock<INotifyUserStrategy>();

        var nus = nusMock.Object;
        var expectedCategoryType = string.Empty;
        var expectedNotificationType = "data_deleted";
        var expectedId = string.Empty;
        var expectedCustomData = new { };

        await sut.SendDeletedNotificationAsync(nus, expectedCategoryType, expectedId, expectedCustomData);

        var validator = SetValidator()
            .HasNotificationType(expectedNotificationType)
            .HasContent(expectedCategoryType, expectedId, expectedCustomData);

        nusMock.Verify(x => x.Notify(It.Is<Notification<ModifiedContentWithCustomData>>((notification) => validator.Validate(notification)), repo), Times.Once);
    }

    private static NotificationValidator SetValidator()
    {
        return new NotificationValidator();
    }
}
