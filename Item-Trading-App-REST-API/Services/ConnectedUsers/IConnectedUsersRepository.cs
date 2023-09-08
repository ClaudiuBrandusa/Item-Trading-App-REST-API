using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.ConnectedUsers
{
    public interface IConnectedUsersRepository
    {
        Task AddConnectionIdToUser(string connectionId, string userId, string userName);

        Task RemoveConnectionIdFromUser(string connectionId, string userId);
    }
}
