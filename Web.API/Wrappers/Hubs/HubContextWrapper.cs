using Infrastructure.Wrappers.Hubs;
using Item_Trading_App_REST_API.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Wrappers.Hubs;

public class HubContextWrapper : IHubContextWrapper
{
    private readonly IHubContext<NotificationHubBase> _hubContext;

    public HubContextWrapper(IHubContext<NotificationHubBase> hubContext)
    {
        _hubContext = hubContext;
    }

    public IHubClientWrapper GetClient(string clientId)
    {
        return new HubClientWrapper(GetClientProxy(clientId));
    }

    public IHubClientWrapper GetClients(string[] clientIds)
    {
        return new HubClientWrapper(GetClientsProxy(clientIds));
    }

    public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
    {
        return _hubContext.Groups.AddToGroupAsync(connectionId, groupName, cancellationToken);
    }

    public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
    {
        return _hubContext.Groups.RemoveFromGroupAsync(connectionId, groupName, cancellationToken);
    }

    public Task NotifyUserAsync(string userId, object notification)
    {
        var client = GetClientProxy(userId);

        return client.SendAsync("notify", notification);
    }

    public Task NotifyUsersAsync(string[] userIds, object notification)
    {
        var clients = GetClientsProxy(userIds);

        return clients.SendAsync("notify", notification);
    }

    private IClientProxy GetClientProxy(string clientId) =>
        _hubContext.Clients.Group(clientId);

    private IClientProxy GetClientsProxy(string[] clientIds) =>
        _hubContext.Clients.Groups(clientIds);
}
