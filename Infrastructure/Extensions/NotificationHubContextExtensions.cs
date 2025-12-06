using Item_Trading_App_REST_API.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Infrastructure.Extensions;

public static class NotificationHubContextExtensions
{
    public static Task NotifyUserAsync(this IHubContext<NotificationHubBase> hubContext, string userId, object notification)
    {
        return hubContext.Clients.Group(userId).SendAsync("notify", notification);
    }

    public static Task NotifyUsersAsync(this IHubContext<NotificationHubBase> hubContext, string[] userIds, object notification)
    {
        return hubContext.Clients.Groups(userIds).SendAsync("notify", notification);
    }
}
