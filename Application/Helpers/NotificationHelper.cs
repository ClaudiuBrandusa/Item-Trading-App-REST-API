using Application.Builders;
using Application.Models.Notifications;
using Application.Utils.Notifications;

namespace Application.Helpers;

public static class NotificationHelper
{
    public static INotifyUserStrategy CreateSingleUserNotificationStrategy(string userId)
    {
        var notificationStrategy = new NotifyUserStrategyBuilder()
            .SetNotificationTargetType(NotificationTarget.SingleUser)
            .SetDestination(userId)
            .Build();

        return notificationStrategy;
    }

    public static INotifyUserStrategy CreateAllUsersNotificationStrategy()
    {
        var notificationStrategy = new NotifyUserStrategyBuilder()
            .SetNotificationTargetType(NotificationTarget.AllUsers)
            .Build();

        return notificationStrategy;
    }

    public static INotifyUserStrategy CreateAllUsersExceptNotificationStrategy(string userId)
    {
        var notificationStrategy = new NotifyUserStrategyBuilder()
            .SetNotificationTargetType(NotificationTarget.AllExcept)
            .SetDestination(userId)
            .Build();

        return notificationStrategy;
    }

    public static INotifyUserStrategy CreateMultipleUsersNotificationStrategy(string[] userIds)
    {
        var notificationStrategy = new NotifyUserStrategyBuilder()
            .SetNotificationTargetType(NotificationTarget.MultipleUsers)
            .SetDestinations(userIds)
            .Build();

        return notificationStrategy;
    }
}
