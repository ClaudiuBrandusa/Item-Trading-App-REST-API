using Application.Services.ConnectedUsers;
using Application.Services.Notification;
using Item_Trading_App_REST_API.Hubs;

namespace Infrastructure.IntegrationTests.Hub;

public class NotificationHubImpl : NotificationHubBase
{
    public NotificationHubImpl(IConnectedUsersRepository connectedUsersRepository, IClientNotificationService clientNotificationService) : base(connectedUsersRepository, clientNotificationService)
    {
    }

    public string GetServerConnectionId() => Context.ConnectionId;

    protected override string GetCurrentUserId()
    {
        return Context.UserIdentifier ?? string.Empty;
    }

    protected override string GetCurrentUserName()
    {
        return Context.User?.Claims.FirstOrDefault(x => x.Type.EndsWith("name"))?.Value ?? string.Empty;
    }
}
