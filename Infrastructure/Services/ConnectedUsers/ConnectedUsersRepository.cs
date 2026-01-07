using Application.Constants;
using Application.Services.Cache;
using Application.Services.ConnectedUsers;
using Infrastructure.Wrappers.Hubs;
using System.Collections.Concurrent;

namespace Infrastructure.Services.ConnectedUsers;

public class ConnectedUsersRepository : IConnectedUsersRepository
{
    private readonly ICacheService _cacheService;
    private readonly IHubContextWrapper _hubContextWrapper; 
    private readonly ConcurrentDictionary<string, HashSet<string>> currentUsersConnections = new();
    private readonly ConcurrentDictionary<string, object> _locks = new();

    public ConnectedUsersRepository(ICacheService cacheService, IHubContextWrapper hubContextWrapper)
    {
        _cacheService = cacheService;
        _hubContextWrapper = hubContextWrapper;
    }

    public string[] ListUserIds()
    {
        return GetKeysArray();
    }

    public string[] ListConnectionIdsForUserId(string userId)
    {
        return GetArrayForKey(userId);
    }

    public async Task<bool> AddConnectionIdToUser(string connectionId, string userId, string userName)
    {
        bool isFirstConnection = false;

        if (Contains(userId))
        {
            Add(userId, connectionId);
                
            isFirstConnection = true;
        }
        else
        {
            Add(userId, connectionId);
                
            await _cacheService.SetCacheValueAsync(CacheKeys.Identity.GetActiveUserKey(userId), userName);
        }

        await _hubContextWrapper.AddToGroupAsync(connectionId, userId, CancellationToken.None);
        return isFirstConnection;
    }

    public async Task RemoveConnectionIdFromUser(string connectionId, string userId)
    {
        if (Contains(userId))
        {
            Remove(userId, connectionId, out var empty);
            if (empty)
            {
                await _cacheService.ClearCacheKeyAsync(CacheKeys.Identity.GetActiveUserKey(userId));
                currentUsersConnections.TryRemove(userId, out _);
            }
        }
        else
        {
            // if this point was reached, then something went wrong
        }

        await _hubContextWrapper.RemoveFromGroupAsync(connectionId, userId);
    }

    public async Task NotifyUserAsync(string userId, object notification)
    {
        if (!Contains(userId))
        {
            return;
        }

        await _hubContextWrapper.NotifyUserAsync(userId, notification);
    }

    public async Task NotifyUsersAsync(object notification)
    {
        await NotifyUsersAsync(GetKeysArray(), notification);
    }

    public async Task NotifyUsersAsync(string[] userIds, object notification)
    {
        await _hubContextWrapper.NotifyUsersAsync(userIds, notification);
    }

    public async Task NotifyAllUsersExceptAsync(string userId, object notification)
    {
        var keys = GetKeysArray().Where(x => !x.Equals(userId)).ToArray();

        await NotifyUsersAsync(keys, notification);
    }

    private string[] GetKeysArray()
    {
        return SnapshotKeys();
    }
    
    private string[] GetArrayForKey(string userId)
    {
        return Snapshot(userId);
    }

    private bool UserIdExists(string userId)
    {
        return currentUsersConnections.ContainsKey(userId);
    }

    private object GetKeyLock(string key) => _locks.GetOrAdd(key, _ => new object());

    private bool Add(string key, string value)
    {
        lock (GetKeyLock(key))
        {
            var set = currentUsersConnections.GetOrAdd(key, _ => new HashSet<string>(StringComparer.Ordinal));

            return set.Add(value);
        }
    }

    private bool Remove(string key, string value, out bool empty)
    {
        lock (GetKeyLock(key))
        {
            empty = false;

            if (!currentUsersConnections.TryGetValue(key, out var set))
                return false;

            var removed = set.Remove(value);

            // optional cleanup when empty
            if (removed && set.Count == 0)
            {
                empty = true;
                currentUsersConnections.TryRemove(key, out _);
                _locks.TryRemove(key, out _);
            }

            return removed;
        }
    }

    private bool Contains(string key, string value)
    {
        lock (GetKeyLock(key))
        {
            return currentUsersConnections.TryGetValue(key, out var set) && set.Contains(value);
        }
    }

    private bool Contains(string key)
    {
        lock (GetKeyLock(key))
        {
            return currentUsersConnections.TryGetValue(key, out var set) && set != null;
        }
    }

    private string[] SnapshotKeys()
    {
        return currentUsersConnections.Keys.ToArray();
    }

    private string[] Snapshot(string key)
    {
        lock (GetKeyLock(key))
        {
            return currentUsersConnections.TryGetValue(key, out var set)
                ? set.ToArray()
                : Array.Empty<string>();
        }
    }
}
