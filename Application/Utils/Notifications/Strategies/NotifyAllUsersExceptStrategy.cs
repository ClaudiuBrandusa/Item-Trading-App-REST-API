using Application.Services.ConnectedUsers;
using Item_Trading_App_Contracts.Notifications;

namespace Application.Utils.Notifications.Strategies;

public class NotifyAllUsersExceptStrategy : INotifyUserStrategy
{
    private readonly string _exceptedUserId;

    public NotifyAllUsersExceptStrategy(string exceptedUserId)
    {
        _exceptedUserId = exceptedUserId;
    }

    public async Task Notify(IClientNotification notification, IConnectedUsersRepository connectedUsersRepository)
    {
        var userIds = connectedUsersRepository.ListUserIds().Where(x => x != _exceptedUserId).ToArray();
        
        if (userIds.Length == 0)
            return;

        await connectedUsersRepository.NotifyUsersAsync(userIds, notification);
    }
}
