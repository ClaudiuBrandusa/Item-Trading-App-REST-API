using Application.Services.ConnectedUsers;

namespace Application.Services.Notification;

public interface INotifyUserStrategy
{
    Task Notify(IHubClientsWrapper hubClientsWrapper, IConnectedUsersRepository connectedUsersRepository, object notification);
}
