using Application.Models.Notifications;
using Application.Services.Notification;

namespace Application.Utils.Notifications;

public static class NotificationHelper
{
    public static INotifyUserStrategy CreateSingleUserNotificationStrategy(string userId)
    {
        var notificationStrategy = new NotifyUserStrategyBuilder()
            .SetNotificationTargetType(NotificationTargetType.SingleUser)
            .SetDestination(userId)
            .Build();

        return notificationStrategy;
    }

    public static INotifyUserStrategy CreateMultipleUsersNotificationStrategy(string[] userIds)
    {
        var notificationStrategy = new NotifyUserStrategyBuilder()
            .SetNotificationTargetType(NotificationTargetType.MultipleUsers)
            .SetDestinations(userIds)
            .Build();

        return notificationStrategy;
    }

    public static INotifyUserStrategy CreateAllUsersNotificationStrategy()
    {
        var notificationStrategy = new NotifyUserStrategyBuilder()
            .SetNotificationTargetType(NotificationTargetType.AllUsers)
            .Build();

        return notificationStrategy;
    }

    public static INotifyUserStrategy CreateAllUsersExceptNotificationStrategy(string exceptedUserId)
    {
        var notificationStrategy = new NotifyUserStrategyBuilder()
            .SetNotificationTargetType(NotificationTargetType.AllExcept)
            .SetDestination(exceptedUserId)
            .Build();

        return notificationStrategy;
    }
}
