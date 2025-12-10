using Application.Services.ConnectedUsers;
using Application.Services.Notification;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Item_Trading_App_REST_API.Hubs;

[Authorize]
public class NotificationHub : NotificationHubBase
{
    public NotificationHub(IConnectedUsersRepository connectedUsersRepository, IClientNotificationService clientNotificationService) : base(connectedUsersRepository, clientNotificationService)
    {
    }

    protected override string GetCurrentUserId()
    {
        return GetUserClaims().FirstOrDefault(c => Equals(c.Type, "id"))?.Value;
    }

    protected override string GetCurrentUserName()
    {
        return GetUserClaims().FirstOrDefault(c => Equals(c.Type, ClaimTypes.NameIdentifier))?.Value;
    }

    private IEnumerable<Claim> GetUserClaims() => Context.User.Claims;
}
