namespace Application.Services.ConnectedUsers;

public interface IConnectedUsersRepository
{
    Task<bool> AddConnectionIdToUser(string connectionId, string userId, string userName);

    Task RemoveConnectionIdFromUser(string connectionId, string userId);

    bool UserExist(string userId);

    bool UsersExist(string[] userIds);

    string[] GetActiveUserIds();
}
