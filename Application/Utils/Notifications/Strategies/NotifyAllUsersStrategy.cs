using Application.Services.ConnectedUsers;
using Item_Trading_App_Contracts.Notifications;
using Item_Trading_App_Contracts.Notifications.Content;

namespace Application.Utils.Notifications.Strategies;

public class NotifyAllUsersStrategy : INotifyUserStrategy
{
    public async Task Notify<T>(Notification<T> notification, IConnectedUsersRepository connectedUsersRepository) where T : NotificationContent
    {
        await connectedUsersRepository.NotifyUsersAsync(notification);
    }
}
