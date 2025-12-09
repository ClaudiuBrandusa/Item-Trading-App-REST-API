using Application.Constants;
using Application.Services.Cache;
using Application.Services.ConnectedUsers;
using Infrastructure.Wrappers.Hubs;

namespace Infrastructure.Services.ConnectedUsers;

public class ConnectedUsersRepository : IConnectedUsersRepository
{
    private readonly ICacheService _cacheService;
    private readonly IHubContextWrapper _hubContextWrapper; 
    private readonly Dictionary<string, List<string>> currentUsersConnections = new();

    public ConnectedUsersRepository(ICacheService cacheService, IHubContextWrapper hubContextWrapper)
    {
        _cacheService = cacheService;
        _hubContextWrapper = hubContextWrapper;
    }

    public string[] ListUserIds()
    {
        return currentUsersConnections.Keys.ToArray();
    }

    public string[] ListConnectionIdsForUserId(string userId)
    {
        if (currentUsersConnections.TryGetValue(userId, out var list))
        {
            return list.ToArray();
        }

        return Array.Empty<string>();
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

        await _hubContextWrapper.AddToGroupAsync(connectionId, userId, CancellationToken.None);
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

        await _hubContextWrapper.RemoveFromGroupAsync(connectionId, userId);
    }

    public Task NotifyUserAsync(string userId, object notification)
    {
        if (!currentUsersConnections.ContainsKey(userId)) return Task.CompletedTask;

        return _hubContextWrapper.NotifyUserAsync(userId, notification);
    }

    public Task NotifyUsersAsync(object notification) =>
        NotifyUsersAsync(currentUsersConnections.Keys.ToArray(), notification);

    public Task NotifyUsersAsync(string[] userIds, object notification) =>
        _hubContextWrapper.NotifyUsersAsync(userIds, notification);

    public Task NotifyAllUsersExceptAsync(string userId, object notification)
    {
        var keys = currentUsersConnections.Keys.Where(x => !x.Equals(userId)).ToArray();

        return NotifyUsersAsync(keys, notification);
    }
}
