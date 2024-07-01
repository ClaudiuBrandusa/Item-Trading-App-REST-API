using Application.Services.ConnectedUsers;
using Application.Services.Notification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Item_Trading_App_REST_API.Hubs;

[Authorize]
public class NotificationHub : NotificationHubBase
{
    private readonly IHttpContextAccessor _httpContext;

    public NotificationHub(IConnectedUsersRepository connectedUsersRepository, IClientNotificationService clientNotificationService, IHttpContextAccessor httpContext) : base(connectedUsersRepository, clientNotificationService)
    {
        _httpContext = httpContext;
    }

    protected override string GetCurrentUserId()
    {
        return GetUserClaims().FirstOrDefault(c => Equals(c.Type, "id"))?.Value;
    }

    protected override string GetCurrentUserName()
    {
        return GetUserClaims().FirstOrDefault(c => Equals(c.Type, ClaimTypes.NameIdentifier))?.Value;
    }

    private IEnumerable<Claim> GetUserClaims() => _httpContext.HttpContext.User.Claims;
}
