using Application.Services.ConnectedUsers;
using Application.Services.Notification;

namespace Application.Utils.Notifications.NotificationStrategies;

public class NotifyAllUsersExceptStrategy : INotifyUserStrategy
{
    string exceptUserId;

    public NotifyAllUsersExceptStrategy(string exceptUserId)
    {
        this.exceptUserId = exceptUserId;
    }

    public Task Notify(IHubClientsWrapper hubClientsWrapper, IConnectedUsersRepository connectedUsersRepository, object notification)
    {
        var keys = connectedUsersRepository.GetActiveUserIds().Where(x => !x.Equals(exceptUserId)).ToArray();

        return hubClientsWrapper.SendTo(keys, Constants.NotificationMessage, notification);
    }
}
