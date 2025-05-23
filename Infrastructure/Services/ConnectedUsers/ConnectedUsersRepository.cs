using Application.Constants;
using Application.Services.Cache;
using Application.Services.ConnectedUsers;
using Item_Trading_App_REST_API.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Infrastructure.Services.ConnectedUsers;

public class ConnectedUsersRepository : IConnectedUsersRepository
{
    private readonly ICacheService _cacheService;
    private readonly IHubContext<NotificationHubBase> hubContext;
    private readonly Dictionary<string, List<string>> currentUsersConnections = new();

    public ConnectedUsersRepository(ICacheService cacheService, IHubContext<NotificationHubBase> hubContext)
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
            await _cacheService.SetCacheValueAsync(CacheKeys.Identity.GetActiveUserKey(userId), userName);
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
                await _cacheService.ClearCacheKeyAsync(CacheKeys.Identity.GetActiveUserKey(userId));
                currentUsersConnections.Remove(userId);
            }
        }
        else
        {
            // if this point was reached, then something went wrong
        }

        await hubContext.Groups.RemoveFromGroupAsync(connectionId, userId);
    }

    public bool UserExist(string userId)
    {
        return currentUsersConnections.ContainsKey(userId);
    }

    public bool UsersExist(string[] userIds)
    {
        return userIds.All(x => currentUsersConnections.ContainsKey(x));
    }

    public string[] GetActiveUserIds()
    {
        return currentUsersConnections.Keys.ToArray();
    }
}
