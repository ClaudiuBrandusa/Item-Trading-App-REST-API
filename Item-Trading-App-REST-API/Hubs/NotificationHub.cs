using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        public override Task OnConnectedAsync()
        {
            var userId = Context.GetHttpContext().User.Claims.FirstOrDefault(c => Equals(c.Type, "id"))?.Value;
            AddConnectionToGroup(userId, Context.ConnectionId);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            var userId = Context.GetHttpContext().User.Claims.FirstOrDefault(c => Equals(c.Type, "id"))?.Value;
            RemoveConnectionFromGroup(userId, Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }

        private void AddConnectionToGroup(string userId, string connectionId)
        {
            Groups.AddToGroupAsync(connectionId, userId);
        }

        private void RemoveConnectionFromGroup(string userId, string connectionId)
        {
            Groups.RemoveFromGroupAsync(connectionId, userId);
        }
    }
}
