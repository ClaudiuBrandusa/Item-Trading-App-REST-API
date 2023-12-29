using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.ConnectedUsers;

public interface IConnectedUsersRepository
{
    Task<bool> AddConnectionIdToUser(string connectionId, string userId, string userName);

    Task RemoveConnectionIdFromUser(string connectionId, string userId);

    Task NotifyUserAsync(string userId, object notification);

    Task NotifyUsersAsync(object notification);

    Task NotifyUsersAsync(string[] userIds, object notification);

    Task NotifyAllUsersExceptAsync(string userId, object notification);
}
