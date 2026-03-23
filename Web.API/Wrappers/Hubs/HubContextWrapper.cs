using Infrastructure.Wrappers.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Item_Trading_App_REST_API.Hubs;

namespace Item_Trading_App_REST_API.Wrappers.Hubs;

public class HubContextWrapper : IHubContextWrapper
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public HubContextWrapper(IHubContext<NotificationHub> hubContext)
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

    public async Task NotifyUserAsync(string userId, object notification)
    {
        var client = GetClientProxy(userId);

        await client.SendAsync("notify", Serialize(notification));
    }

    public async Task NotifyUsersAsync(string[] userIds, object notification)
    {
        var clients = GetClientsProxy(userIds);

        await clients.SendAsync("notify", Serialize(notification));
    }

    private IClientProxy GetClientProxy(string clientId) =>
        _hubContext.Clients.Group(clientId);

    private IClientProxy GetClientsProxy(string[] clientIds) =>
        _hubContext.Clients.Groups(clientIds);

    private string Serialize(object notification) =>
        JsonSerializer.Serialize(notification, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
}
