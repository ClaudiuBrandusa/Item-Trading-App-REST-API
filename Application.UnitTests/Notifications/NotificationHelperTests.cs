using Application.Utils.Notifications;
using Application.Utils.Notifications.NotificationStrategies;
using Domain.Entities.Identity;

namespace Application_UnitTests.Notifications;

public class NotificationHelperTests
{
    [Fact(DisplayName = "Create single user notification strategy using the notification helper")]
    public void CreateSingleUserNotificationStrategy()
    {
        // Arrange

        string userId = User.GenerateId();

        // Act

        var notificationStrategy = NotificationHelper.CreateSingleUserNotificationStrategy(userId);

        // Assert

        Assert.IsType<NotifySingleUserStrategy>(notificationStrategy);
    }

    [Fact(DisplayName = "Create multiple users notification strategy using the notification helper")]
    public void CreateMultipleUsersNotificationStrategy()
    {
        // Arrange

        string[] userIds = new string[] { User.GenerateId(), User.GenerateId() };

        // Act

        var notificationStrategy = NotificationHelper.CreateMultipleUsersNotificationStrategy(userIds);

        // Assert

        Assert.IsType<NotifyMultipleUsersStrategy>(notificationStrategy);
    }

    [Fact(DisplayName = "Create all users notification strategy using the notification helper")]
    public void CreateAllUsersNotificationStrategy()
    {
        // Arrange
        
        // Act

        var notificationStrategy = NotificationHelper.CreateAllUsersNotificationStrategy();

        // Assert

        Assert.IsType<NotifyAllUsersStrategy>(notificationStrategy);
    }

    [Fact(DisplayName = "Create all users notification strategy using the notification helper")]
    public void CreateAllUsersExceptNotificationStrategy()
    {
        // Arrange

        string userId = User.GenerateId();

        // Act

        var notificationStrategy = NotificationHelper.CreateAllUsersExceptNotificationStrategy(userId);

        // Assert

        Assert.IsType<NotifyAllUsersExceptStrategy>(notificationStrategy);
    }
}
