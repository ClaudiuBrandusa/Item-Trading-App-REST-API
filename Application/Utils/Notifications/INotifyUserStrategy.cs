using Application.Services.ConnectedUsers;
using Item_Trading_App_Contracts.Notifications;

namespace Application.Utils.Notifications;

public interface INotifyUserStrategy
{
    Task Notify(IClientNotification notification, IConnectedUsersRepository connectedUsersRepository);
}
