using Application.Services.ConnectedUsers;
using Item_Trading_App_Contracts.Notifications;

namespace Application.Utils.Notifications.Strategies;

public class NotifySingleUserStrategy : INotifyUserStrategy
{
    private string targetUserId;

    public NotifySingleUserStrategy(string targetUserId)
    {
        this.targetUserId = targetUserId;
    }

    public async Task Notify(IClientNotification notification, IConnectedUsersRepository connectedUsersRepository)
    {
        if (string.IsNullOrEmpty(targetUserId))
            return;

        await connectedUsersRepository.NotifyUserAsync(targetUserId, notification);
    }
}
