using Application.Services.ConnectedUsers;
using Item_Trading_App_Contracts.Notifications;
using Item_Trading_App_Contracts.Notifications.Content;

namespace Application.Utils.Notifications.Strategies;

public class NotifyMultipleUsersStrategy : INotifyUserStrategy
{
    private string[] targetUserIds;

    public NotifyMultipleUsersStrategy(string[] tragetUserIds)
    {
        this.targetUserIds = tragetUserIds;
    }

    public async Task Notify<T>(Notification<T> notification, IConnectedUsersRepository connectedUsersRepository) where T : NotificationContent
    {
        if (targetUserIds.Length == 0)
            return;

        await connectedUsersRepository.NotifyUsersAsync(targetUserIds, notification);
    }
}
