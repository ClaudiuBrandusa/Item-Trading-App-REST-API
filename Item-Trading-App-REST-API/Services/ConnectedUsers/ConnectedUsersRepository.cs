using Item_Trading_App_REST_API.Constants;
using Item_Trading_App_REST_API.Hubs;
using Item_Trading_App_REST_API.Services.Cache;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.ConnectedUsers;

public class ConnectedUsersRepository : IConnectedUsersRepository
{
    private readonly ICacheService _cacheService;
    private readonly IHubContext<NotificationHub> hubContext;
    private readonly Dictionary<string, List<string>> currentUsersConnections = new();

    public ConnectedUsersRepository(ICacheService cacheService, IHubContext<NotificationHub> hubContext)
    {
        _cacheService = cacheService;
        this.hubContext = hubContext;
    }

    public async Task<bool> AddConnectionIdToUser(string connectionId, string userId, string userName)
    {
        bool isFirstConnection = false;

        if (currentUsersConnections.ContainsKey(userId)) 
        {
            currentUsersConnections[userId].Add(connectionId);
            isFirstConnection = true;
        }
        else
        {
            currentUsersConnections.Add(userId, new List<string>() { connectionId });
            await _cacheService.SetCacheValueAsync(string.Concat(CachePrefixKeys.ActiveUsers, userId), userName);
        }

        await hubContext.Groups.AddToGroupAsync(connectionId, userId);
        return isFirstConnection;
    }

    public async Task RemoveConnectionIdFromUser(string connectionId, string userId)
    {
        if (currentUsersConnections.ContainsKey(userId))
        {
            currentUsersConnections[userId].Remove(connectionId);
            if (currentUsersConnections[userId].Count == 0)
            {
                await _cacheService.ClearCacheKeyAsync(string.Concat(CachePrefixKeys.ActiveUsers, userId));
                currentUsersConnections.Remove(userId);
            }
        } else
        {
            // if this point was reached, then something went wrong
        }

        await hubContext.Groups.RemoveFromGroupAsync(connectionId, userId);
    }

    public Task NotifyUserAsync(string userId, object notification)
    {
        if (!currentUsersConnections.ContainsKey(userId)) return Task.CompletedTask;
        
        return  hubContext.Clients.Group(userId).SendAsync("notify", notification);
    }

    public Task NotifyUsersAsync(object notification) =>
        NotifyUsersAsync(currentUsersConnections.Keys.ToList(), notification);

    public Task NotifyUsersAsync(List<string> userIds, object notification) =>
        hubContext.Clients.Groups(userIds).SendAsync("notify", notification);

    public Task NotifyAllUsersExceptAsync(string userId, object notification)
    {
        var keys = currentUsersConnections.Keys.Where(x => !x.Equals(userId)).ToList();

        return NotifyUsersAsync(keys, notification);
    }
}
