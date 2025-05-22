namespace Application.Utils.Notifications;

public interface INotifyUserStrategy
{
    Task Notify(IHubClientsWrapper hubClientsWrapper, object notification);
}
