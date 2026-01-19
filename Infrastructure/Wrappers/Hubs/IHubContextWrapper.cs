namespace Infrastructure.Wrappers.Hubs;

public interface IHubContextWrapper
{
    IHubClientWrapper GetClient(string clientId);

    IHubClientWrapper GetClients(string[] clientIds);

    Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default);

    Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default);

    Task NotifyUserAsync(string userId, object notification);

    Task NotifyUsersAsync(string[] userIds, object notification);
}
