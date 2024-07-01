using Application.Services.ConnectedUsers;
using Application.Services.Notification;
using Microsoft.AspNetCore.SignalR;

namespace Item_Trading_App_REST_API.Hubs;

public abstract class NotificationHubBase : Hub
{
    private readonly IConnectedUsersRepository _connectedUsersRepository;
    private readonly IClientNotificationService _clientNotificationService;

    public NotificationHubBase(IConnectedUsersRepository connectedUsersRepository, IClientNotificationService clientNotificationService)
    {
        _connectedUsersRepository = connectedUsersRepository;
        _clientNotificationService = clientNotificationService;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetCurrentUserId();
        var name = GetCurrentUserName();

        if (!await _connectedUsersRepository.AddConnectionIdToUser(Context.ConnectionId, userId, name))
            await _clientNotificationService.SendMessageNotificationToAllUsersExceptAsync(userId, $"User {name} has connected!", DateTime.Now);
        await _clientNotificationService.SendMessageNotificationToUserAsync(userId, "Welcome!", DateTime.Now);

        await base.OnConnectedAsync();
    }
    public override Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetCurrentUserId();
        
        _connectedUsersRepository.RemoveConnectionIdFromUser(Context.ConnectionId, userId);
        return base.OnDisconnectedAsync(exception);
}

    protected abstract string GetCurrentUserId();

    protected abstract string GetCurrentUserName();
}
