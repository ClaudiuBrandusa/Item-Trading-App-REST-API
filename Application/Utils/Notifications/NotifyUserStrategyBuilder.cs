using Application.Models.Notifications;
using Application.Services.Notification;
using Application.Utils.Notifications.NotificationStrategies;

namespace Application.Utils.Notifications;

public class NotifyUserStrategyBuilder
{
    public NotificationTargetType? NotificationTarget { get; set; }

    private List<string> destinations = new List<string>();

    public NotifyUserStrategyBuilder SetNotificationTargetType(NotificationTargetType notificationTargetType)
    {
        if (NotificationTarget is not null)
        {
            throw new ArgumentException($"{nameof(NotificationTarget)} was already set.");
        }

        NotificationTarget = notificationTargetType;

        return this;
    }

    public NotifyUserStrategyBuilder SetDestination(string destination)
    {
        if (string.IsNullOrEmpty(destination))
        {
            throw new ArgumentException("Can't set the notification destination as null or empty.");
        }

        destinations.Add(destination);
        return this;
    }

    public NotifyUserStrategyBuilder SetDestinations(string[] destinations)
    {
        if (destinations is null || destinations.Length == 0)
        {
            throw new ArgumentException("Can't set the notification destinations as null or empty.");
        }

        this.destinations.AddRange(destinations);
        return this;
    }

    public INotifyUserStrategy Build()
    {
        if (NotificationTarget is null)
            throw new ArgumentException("Unable to build the notification strategy due to not having a notification target.");

        if (NotificationTarget == NotificationTargetType.SingleUser)
        {
            if (destinations.Count == 0)
                throw new ArgumentException("Unable to send a notification to single user if no user id was set as destination.");
            return new NotifySingleUserStrategy(destinations[0]);
        }

        if (NotificationTarget == NotificationTargetType.MultipleUsers)
        {
            if (destinations.Count == 0)
                throw new ArgumentException("Unable to send a notification to multiple users when no user id was set as destination.");
            return new NotifyMultipleUsersStrategy(destinations.ToArray());
        }

        if (NotificationTarget == NotificationTargetType.AllUsers)
        {
            return new NotifyAllUsersStrategy();
        }

        if (NotificationTarget == NotificationTargetType.AllExcept)
        {
            if (destinations.Count == 0)
                throw new ArgumentException("Unable to except sending to a user if there was no user id added");

            return new NotifyAllUsersExceptStrategy(destinations[0]);
        }

        throw new ArgumentException("Invalid data, unable to build a notification strategy due to not having enough data.");
    }
}
