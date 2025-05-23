using Application.Services.ConnectedUsers;
using Application.Services.Notification;

namespace Application.Utils.Notifications.NotificationStrategies;

public class NotifyMultipleUsersStrategy : INotifyUserStrategy
{
    private string[] tragetUserIds;

    public NotifyMultipleUsersStrategy(string[] tragetUserIds)
    {
        this.tragetUserIds = tragetUserIds;
    }

    public async Task Notify(IHubClientsWrapper hubClientsWrapper, IConnectedUsersRepository connectedUsersRepository, object notification)
    {
        if (!connectedUsersRepository.UsersExist(tragetUserIds))
            return;

        await hubClientsWrapper.SendTo(tragetUserIds, Constants.NotificationMessage, notification);
    }
}
