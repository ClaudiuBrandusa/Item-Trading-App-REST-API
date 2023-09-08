using Item_Trading_App_REST_API.Constants;
using Item_Trading_App_REST_API.Hubs;
using Item_Trading_App_REST_API.Services.Cache;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.ConnectedUsers
{
    public class ConnectedUsersRepository : IConnectedUsersRepository
    {
        private readonly ICacheService _cacheService;
        private readonly IHubContext<NotificationHub> hubContext;
        private Dictionary<string, List<string>> currentUsersConnections = new Dictionary<string, List<string>>();

        public ConnectedUsersRepository(ICacheService cacheService, IHubContext<NotificationHub> hubContext)
        {
            _cacheService = cacheService;
            this.hubContext = hubContext;
        }

        public async Task AddConnectionIdToUser(string connectionId, string userId, string userName)
        {
            if (currentUsersConnections.ContainsKey(userId)) 
            {
                currentUsersConnections[userId].Add(connectionId);
            }
            else
            {
                currentUsersConnections.Add(userId, new List<string>() { connectionId });
                await _cacheService.SetCacheValueAsync(string.Concat(CachePrefixKeys.ActiveUsers, userId), userName);
            }

            await hubContext.Groups.AddToGroupAsync(connectionId, userId);
        }

        public async Task RemoveConnectionIdFromUser(string connectionId, string userId)
        {
            if (currentUsersConnections[userId].Count == 1) await _cacheService.ClearCacheKeyAsync(string.Concat(CachePrefixKeys.ActiveUsers, userId));
            if (currentUsersConnections.ContainsKey(userId))
            {
                currentUsersConnections[userId].Remove(connectionId);
            } else
            {
                // if this point was reached, then something went wrong
            }


            await hubContext.Groups.RemoveFromGroupAsync(connectionId, userId);
        }

        #region Private



        #endregion Private
    }
}
