using Application.Services.ConnectedUsers;
using Application.Services.Notification;

namespace Application.Utils.Notifications.NotificationStrategies;

public class NotifyAllUsersStrategy : INotifyUserStrategy
{
    public async Task Notify(IHubClientsWrapper hubClientsWrapper, IConnectedUsersRepository connectedUsersRepository, object notification)
    {
        await hubClientsWrapper.SendTo(connectedUsersRepository.GetActiveUserIds(), Constants.NotificationMessage, notification);
    }
}
