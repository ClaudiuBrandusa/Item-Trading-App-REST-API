using Application.Services.ConnectedUsers;
using Item_Trading_App_Contracts.Notifications;

namespace Application.Utils.Notifications.Strategies;

public class NotifyMultipleUsersStrategy : INotifyUserStrategy
{
    private string[] targetUserIds;

    public NotifyMultipleUsersStrategy(string[] tragetUserIds)
    {
        this.targetUserIds = tragetUserIds;
    }

    public async Task Notify(IClientNotification notification, IConnectedUsersRepository connectedUsersRepository)
    {
        if (targetUserIds.Length == 0)
            return;

        await connectedUsersRepository.NotifyUsersAsync(targetUserIds, notification);
    }
}
