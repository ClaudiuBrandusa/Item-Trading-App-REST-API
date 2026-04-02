using Application.Constants;
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
        var expectedNotificationType = NotificationTypes.Created;
        var expectedId = string.Empty;

        await sut.SendCreatedNotificationAsync(nus, expectedCategoryType, expectedId);

        var validator = SetValidator<MockData>()
            .HasNotificationType(expectedNotificationType)
            .HasContent(expectedCategoryType, expectedId);

        nusMock.Verify(x => x.Notify(It.Is<Notification<ModifiedContent>>((notification) => validator.Validate(notification)), repo), Times.Once);
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
        var expectedNotificationType = NotificationTypes.Created;
        var expectedId = string.Empty;
        var expectedCustomData = new MockData { Value = 123 };
        var customDataType = expectedCustomData.GetType();

        await sut.SendCreatedNotificationAsync(nus, expectedCategoryType, expectedId, expectedCustomData);

        var validator = SetValidator<MockData>()
            .HasNotificationType(expectedNotificationType)
            .HasContent(expectedCategoryType, expectedId)
            .HasCustomDataValidationRule((data) => data is not null && data.Value == expectedCustomData.Value);

        nusMock.Verify(x => x.Notify(It.Is<Notification<ModifiedContentJson>>((notification) => validator.Validate(notification)), repo), Times.Once);
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
        var expectedNotificationType = NotificationTypes.Information;
        var expectedId = string.Empty;

        await sut.SendMessageNotificationAsync(nus, expectedCategoryType, expectedDateTime);

        var validator = SetValidator<MockData>()
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
        var expectedNotificationType = NotificationTypes.Changed;
        var expectedId = string.Empty;

        await sut.SendUpdatedNotificationAsync(nus, expectedCategoryType, expectedId);

        var validator = SetValidator<MockData>()
            .HasNotificationType(expectedNotificationType)
            .HasContent(expectedCategoryType, expectedId);

        nusMock.Verify(x => x.Notify(It.Is<Notification<ModifiedContent>>((notification) => validator.Validate(notification)), repo), Times.Once);
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
        var expectedNotificationType = NotificationTypes.Changed;
        var expectedId = string.Empty;
        var expectedCustomData = new MockData { Value = 123 };

        await sut.SendUpdatedNotificationAsync(nus, expectedCategoryType, expectedId, expectedCustomData);

        var validator = SetValidator<MockData>()
            .HasNotificationType(expectedNotificationType)
            .HasContent(expectedCategoryType, expectedId)
            .HasCustomDataValidationRule((data) => data is not null && data.Value == expectedCustomData.Value);

        nusMock.Verify(x => x.Notify(It.Is<Notification<ModifiedContentJson>>((notification) => validator.Validate(notification)), repo), Times.Once);
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
        var expectedNotificationType = NotificationTypes.Deleted;
        var expectedId = string.Empty;

        await sut.SendDeletedNotificationAsync(nus, expectedCategoryType, expectedId);

        var validator = SetValidator<MockData>()
            .HasNotificationType(expectedNotificationType)
            .HasContent(expectedCategoryType, expectedId);

        nusMock.Verify(x => x.Notify(It.Is<Notification<ModifiedContent>>((notification) => validator.Validate(notification)), repo), Times.Once);
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
        var expectedNotificationType = NotificationTypes.Deleted;
        var expectedId = string.Empty;
        var expectedCustomData = new MockData { Value = 123 };

        await sut.SendDeletedNotificationAsync(nus, expectedCategoryType, expectedId, expectedCustomData);

        var validator = SetValidator<MockData>()
            .HasNotificationType(expectedNotificationType)
            .HasContent(expectedCategoryType, expectedId)
            .HasCustomDataValidationRule((data) => data is not null && data.Value == expectedCustomData.Value);

        nusMock.Verify(x => x.Notify(It.Is<Notification<ModifiedContentJson>>((notification) => validator.Validate(notification)), repo), Times.Once);
    }

    public class MockData
    {
        public int Value { get; set; }
    }

    private static NotificationValidator<T> SetValidator<T>()
    {
        return new NotificationValidator<T>();
    }
}
