using Application.Constants;
using Application.Services.Cache;
using Application.Services.ConnectedUsers;
using Application.Utils.Notifications;
using Item_Trading_App_REST_API.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Infrastructure.Services.ConnectedUsers;

public class ConnectedUsersRepository : IConnectedUsersRepository
{
    private readonly ICacheService _cacheService;
    private readonly IHubContext<NotificationHubBase> hubContext;
    private readonly Dictionary<string, List<string>> currentUsersConnections = new();
    private readonly IHubClientsWrapper hubClientsWrapper;

    public ConnectedUsersRepository(ICacheService cacheService, IHubContext<NotificationHubBase> hubContext, IHubClientsWrapper hubClientsWrapper)
    {
        _cacheService = cacheService;
        this.hubContext = hubContext;
        this.hubClientsWrapper = hubClientsWrapper;
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

    public class NotifySingleUserStrategy : INotifyUserStrategy
    {
        private string targetUserId;

        public NotifySingleUserStrategy(string targetUserId)
        {
            this.targetUserId = targetUserId;
        }

        public Task Notify(IHubClientsWrapper hubClientsWrapper, Dictionary<string, List<string>> currentUsersConnections, object notification)
        {
            if (!currentUsersConnections.ContainsKey(targetUserId)) return Task.CompletedTask;
            
            return hubClientsWrapper.SendTo(targetUserId, "notify", notification);
        }
    }

    public class NotifyMultipleUsersStrategy : INotifyUserStrategy
    {
        private string[] tragetUserIds;

        public NotifyMultipleUsersStrategy(string[] tragetUserIds)
        {
            this.tragetUserIds = tragetUserIds;
        }

        public Task Notify(IHubClientsWrapper hubClientsWrapper, object notification)
        {
            return hubClientsWrapper.SendTo(tragetUserIds, "notify", notification);
        }
    }

    public class NotifyAllUsersStrategy : INotifyUserStrategy
    {
        public Task Notify(IHubClientsWrapper hubClientsWrapper, object notification)
        {
            return hubClientsWrapper.SendTo(currentUsersConnections.Keys.ToArray(), "notify", notification);
        }
    }

    public class NotifyAllUsersExceptStrategy : INotifyUserStrategy
    {
        string exceptUserId;

        public NotifyAllUsersExceptStrategy(string exceptUserId)
        {
            this.exceptUserId = exceptUserId;
        }

        public Task Notify(IHubClientsWrapper hubClientsWrapper, object notification)
        {
            var keys = currentUsersConnections.Keys.Where(x => !x.Equals(exceptUserId)).ToArray();

            return hubClientsWrapper.SendTo(keys, "notify", notification);
        }
    }

    public enum NotificationTargetType
    {
        SingleUser,
        MultipleUsers,
        AllUsers,
        AllExcept
    }

    public class NotifyUserStrategyBuilder
    {
        public NotificationTargetType? NotificationTargetType { get; set; }

        private List<string> destinations = new List<string>();

        public NotifyUserStrategyBuilder SetNotificationTargetType(NotificationTargetType notificationTargetType)
        {
            if (NotificationTargetType is not null)
            {
                throw new ArgumentException($"{nameof(NotificationTargetType)} was already set.");
            }

            NotificationTargetType = notificationTargetType;

            return this;
        }

        public NotifyUserStrategyBuilder SetDestination(string destination)
        {
            destinations.Add(destination);
            return this;
        }

        public NotifyUserStrategyBuilder SetDestinations(string[] destinations)
        {
            this.destinations.AddRange(destinations);
            return this;
        }

        public INotifyUserStrategy Build()
        {
            if (NotificationTargetType is null)
                throw new ArgumentException("Unable to build the notification strategy due to not having a notification target.");

            if (NotificationTargetType == ConnectedUsersRepository.NotificationTargetType.SingleUser)
            {
                if (destinations.Count == 0)
                    throw new ArgumentException("Unable to send a notification to single user if no user id was set as destination.");

                return new NotifySingleUserStrategy(destinations[0]);
            }

            if (NotificationTargetType == ConnectedUsersRepository.NotificationTargetType.MultipleUsers)
            {
                if (destinations.Count == 0)
                    throw new ArgumentException("Unable to send a notification to multiple users when no user id was set as destination.");

                return new NotifyMultipleUsersStrategy(destinations.ToArray());
            }

            if (NotificationTargetType == ConnectedUsersRepository.NotificationTargetType.AllUsers)
                return new NotifyAllUsersStrategy();

            if (NotificationTargetType == ConnectedUsersRepository.NotificationTargetType.AllExcept)
            {
                if (destinations.Count == 0)
                    throw new ArgumentException("Unable to except sending to a user if there was no user id added");

                return new NotifyAllUsersExceptStrategy(destinations[0]);
            }

            throw new ArgumentException("Invalid data, unable to build a notification strategy due to not having enough data.");
        }
    }

    public Task Notify(INotifyUserStrategy notifyStrategy, object notification)
    {
        return notifyStrategy.Notify(hubClientsWrapper, notification);
    }

    public Task NotifyUserAsync(string userId, object notification)
    {
        if (!currentUsersConnections.ContainsKey(userId)) return Task.CompletedTask;

        return hubContext.Clients.Group(userId).SendAsync("notify", notification);
    }

    public Task NotifyUsersAsync(object notification) =>
        NotifyUsersAsync(currentUsersConnections.Keys.ToArray(), notification);

    public Task NotifyUsersAsync(string[] userIds, object notification) =>
        hubContext.Clients.Groups(userIds).SendAsync("notify", notification);

    public Task NotifyAllUsersExceptAsync(string userId, object notification)
    {
        var keys = currentUsersConnections.Keys.Where(x => !x.Equals(userId)).ToArray();

        return NotifyUsersAsync(keys, notification);
    }
}
