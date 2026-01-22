using Application.Services.ConnectedUsers;
using Item_Trading_App_Contracts.Notifications;
using Item_Trading_App_Contracts.Notifications.Content;

namespace Application.Utils.Notifications.Strategies;

public class NotifyAllUsersExceptStrategy : INotifyUserStrategy
{
    private readonly string _exceptedUserId;

    public NotifyAllUsersExceptStrategy(string exceptedUserId)
    {
        _exceptedUserId = exceptedUserId;
    }

    public async Task Notify<T>(Notification<T> notification, IConnectedUsersRepository connectedUsersRepository) where T : NotificationContent
    {
        var userIds = connectedUsersRepository.ListUserIds().Where(x => x != _exceptedUserId).ToArray();
        
        if (userIds.Length == 0)
            return;

        await connectedUsersRepository.NotifyUsersAsync(userIds, notification);
    }
}
