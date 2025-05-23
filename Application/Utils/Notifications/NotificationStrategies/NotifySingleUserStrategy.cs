using Application.Services.ConnectedUsers;
using Application.Services.Notification;

namespace Application.Utils.Notifications.NotificationStrategies;

public class NotifySingleUserStrategy : INotifyUserStrategy
{
    private string targetUserId;

    public NotifySingleUserStrategy(string targetUserId)
    {
        this.targetUserId = targetUserId;
    }

    public async Task Notify(IHubClientsWrapper hubClientsWrapper, IConnectedUsersRepository connectedUsersRepository, object notification)
    {
        if (!connectedUsersRepository.UserExist(targetUserId))
            return;

        await hubClientsWrapper.SendTo(targetUserId, Constants.NotificationMessage, notification);
    }
}
