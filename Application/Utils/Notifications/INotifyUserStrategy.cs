namespace Application.Utils.Notifications;

public interface INotifyUserStrategy
{
    Task Notify(IHubClients hubClients, object notification);
}
