using Application.Services.ConnectedUsers;
using Item_Trading_App_Contracts.Notifications;
using Item_Trading_App_Contracts.Notifications.Content;

namespace Application.Utils.Notifications.Strategies;

public class NotifyMultipleUsersStrategy : INotifyUserStrategy
{
    private string[] tragetUserIds;

    public NotifyMultipleUsersStrategy(string[] tragetUserIds)
    {
        this.tragetUserIds = tragetUserIds;
    }

    public Task Notify<T>(Notification<T> notification, IConnectedUsersRepository connectedUsersRepository) where T : NotificationContent
    {
        return connectedUsersRepository.NotifyUsersAsync(tragetUserIds, notification);
    }
}
