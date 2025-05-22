using Application.Utils.Notifications;
using Item_Trading_App_REST_API.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Infrastructure.Services.Notification;

public class HubClientsWrapper : IHubClientsWrapper
{
    private readonly IHubContext<NotificationHubBase> hubContext;

    public HubClientsWrapper(IHubContext<NotificationHubBase> hubContext)
    {
        this.hubContext = hubContext;
    }

    public Task SendTo(string target, string message, object content)
    {
        return hubContext.Clients.Group(target).SendAsync(message, content);
    }

    public Task SendTo(string[] targets, string message, object content)
    {
        return hubContext.Clients.Groups(targets).SendAsync(message, content);
    }
}
