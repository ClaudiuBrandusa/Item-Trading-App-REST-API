using Application.Services.ConnectedUsers;
using Item_Trading_App_Contracts.Notifications;
using Item_Trading_App_Contracts.Notifications.Content;

namespace Application.Utils.Notifications.Strategies;

public class NotifySingleUserStrategy : INotifyUserStrategy
{
    private string targetUserId;

    public NotifySingleUserStrategy(string targetUserId)
    {
        this.targetUserId = targetUserId;
    }

    public async Task Notify<T>(Notification<T> notification, IConnectedUsersRepository connectedUsersRepository) where T : NotificationContent
    {
        await connectedUsersRepository.NotifyUserAsync(targetUserId, notification);
    }
}
