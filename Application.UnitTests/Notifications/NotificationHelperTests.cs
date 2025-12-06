using Application.Helpers;
using Application.Utils.Notifications.Strategies;

namespace Application_UnitTests.Notifications;

public class NotificationHelperTests
{
    [Fact]
    public void CreateSingleUserNotificationStrategy_ShouldReturnStrategyWithSingleUserTarget()
    {
        // Arrange
        var userId = "testUserId";

        // Act
        var strategy = NotificationHelper.CreateSingleUserNotificationStrategy(userId);

        // Assert
        Assert.NotNull(strategy);
        Assert.IsAssignableFrom<NotifySingleUserStrategy>(strategy);
    }

    [Fact]
    public void CreateAllUsersNotificationStrategy_ShouldReturnStrategyWithAllUsersTarget()
    {
        // Act
        var strategy = NotificationHelper.CreateAllUsersNotificationStrategy();

        // Assert
        Assert.NotNull(strategy);
        Assert.IsAssignableFrom<NotifyAllUsersStrategy>(strategy);
    }

    [Fact]
    public void CreateAllUsersExceptNotificationStrategy_ShouldReturnStrategyWithAllExceptTarget()
    {
        // Arrange
        var userId = "testUserId";

        // Act
        var strategy = NotificationHelper.CreateAllUsersExceptNotificationStrategy(userId);

        // Assert
        Assert.NotNull(strategy);
        Assert.IsAssignableFrom<NotifyAllUsersExceptStrategy>(strategy);
    }

    [Fact]
    public void CreateMultipleUsersNotificationStrategy_WithEmptyArray_ShouldReturnStrategy()
    {
        // Arrange
        var userIds = new string[] { "A", "B", "C" };

        // Act
        var strategy = NotificationHelper.CreateMultipleUsersNotificationStrategy(userIds);

        // Assert
        Assert.NotNull(strategy);
        Assert.IsAssignableFrom<NotifyMultipleUsersStrategy>(strategy);
    }
}
