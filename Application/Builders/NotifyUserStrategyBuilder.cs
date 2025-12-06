using Application.Models.Notifications;
using Application.Utils.Notifications;
using Application.Utils.Notifications.Strategies;

namespace Application.Builders;

public class NotifyUserStrategyBuilder
{
    private NotificationTarget? _notificationTargetType { get; set; }

    private List<string> _destinations = new List<string>();

    public NotifyUserStrategyBuilder SetNotificationTargetType(NotificationTarget notificationTargetType)
    {
        if (_notificationTargetType is not null)
        {
            throw new ArgumentException($"{nameof(notificationTargetType)} was already set.");
        }

        _notificationTargetType = notificationTargetType;

        return this;
    }

    public NotifyUserStrategyBuilder SetDestination(string destination)
    {
        _destinations.Add(destination);
        return this;
    }

    public NotifyUserStrategyBuilder SetDestinations(string[] destinations)
    {
        _destinations.AddRange(destinations);
        return this;
    }

    public INotifyUserStrategy Build()
    {
        if (_notificationTargetType is null)
            throw new ArgumentException("Unable to build the notification strategy due to not having a notification target.");

        if (_notificationTargetType == NotificationTarget.SingleUser)
        {
            if (_destinations.Count == 0)
                throw new ArgumentException("Unable to send a notification to single user if no user id was set as destination.");

            return new NotifySingleUserStrategy(_destinations[0]);
        }

        if (_notificationTargetType == NotificationTarget.MultipleUsers)
        {
            if (_destinations.Count == 0)
                throw new ArgumentException("Unable to send a notification to multiple users when no user id was set as destination.");

            return new NotifyMultipleUsersStrategy(_destinations.ToArray());
        }

        if (_notificationTargetType == NotificationTarget.AllUsers)
            return new NotifyAllUsersStrategy();

        if (_notificationTargetType == NotificationTarget.AllExcept)
        {
            if (_destinations.Count == 0)
                throw new ArgumentException("Unable to send a notification to all users, excepting a specific user, if no user id was set as destination.");

            return new NotifyAllUsersExceptStrategy(_destinations[0]);
        }

        throw new ArgumentException("Invalid data, unable to build a notification strategy due to not having enough data.");
    }
}
