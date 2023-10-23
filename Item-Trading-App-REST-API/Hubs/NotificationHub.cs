﻿using Item_Trading_App_REST_API.Services.ConnectedUsers;
using Item_Trading_App_REST_API.Services.Notification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    private readonly IConnectedUsersRepository _connectedUsersRepository;
    private readonly INotificationService _notificationService;

    public NotificationHub(IConnectedUsersRepository connectedUsersRepository, INotificationService notificationService)
    {
        _connectedUsersRepository = connectedUsersRepository;
        _notificationService = notificationService;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.GetHttpContext().User.Claims.FirstOrDefault(c => Equals(c.Type, "id"))?.Value;
        var name = Context.GetHttpContext().User.Claims.FirstOrDefault(c => Equals(c.Type, ClaimTypes.NameIdentifier))?.Value;

        if(!await _connectedUsersRepository.AddConnectionIdToUser(Context.ConnectionId, userId, name))
            await _notificationService.SendMessageNotificationToAllUsersExceptAsync(userId, $"User {name} has connected!", DateTime.Now);
        await _notificationService.SendMessageNotificationToUserAsync(userId, "Welcome!", DateTime.Now);

        await base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception exception)
    {
        var userId = Context.GetHttpContext().User.Claims.FirstOrDefault(c => Equals(c.Type, "id"))?.Value;
        var name = Context.GetHttpContext().User.Claims.FirstOrDefault(c => Equals(c.Type, ClaimTypes.NameIdentifier))?.Value;

        _connectedUsersRepository.RemoveConnectionIdFromUser(Context.ConnectionId, userId);
        return base.OnDisconnectedAsync(exception);
    }
}
