namespace Application.Services.Notification;

public interface IHubClientsWrapper
{
    Task SendTo(string target, string message, object content);

    Task SendTo(string[] targets, string message, object content);
}
