using Item_Trading_App_REST_API.Services.ConnectedUsers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly IConnectedUsersRepository _connectedUsersRepository;

        public NotificationHub(IConnectedUsersRepository connectedUsersRepository)
        {
            _connectedUsersRepository = connectedUsersRepository;
        }

        public override Task OnConnectedAsync()
        {
            var userId = Context.GetHttpContext().User.Claims.FirstOrDefault(c => Equals(c.Type, "id"))?.Value;
            var name = Context.GetHttpContext().User.Claims.FirstOrDefault(c => Equals(c.Type, ClaimTypes.NameIdentifier))?.Value;
            
            _connectedUsersRepository.AddConnectionIdToUser(Context.ConnectionId, userId, name);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            var userId = Context.GetHttpContext().User.Claims.FirstOrDefault(c => Equals(c.Type, "id"))?.Value;
            var name = Context.GetHttpContext().User.Claims.FirstOrDefault(c => Equals(c.Type, ClaimTypes.NameIdentifier))?.Value;

            _connectedUsersRepository.RemoveConnectionIdFromUser(Context.ConnectionId, userId);
            return base.OnDisconnectedAsync(exception);
        }
    }
}
