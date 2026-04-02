using Application.Services.ConnectedUsers;
using Item_Trading_App_Contracts.Notifications;

namespace Application.Utils.Notifications.Strategies;

public class NotifyAllUsersStrategy : INotifyUserStrategy
{
    public async Task Notify(IClientNotification notification, IConnectedUsersRepository connectedUsersRepository)
    {
        await connectedUsersRepository.NotifyUsersAsync(notification);
    }
}
