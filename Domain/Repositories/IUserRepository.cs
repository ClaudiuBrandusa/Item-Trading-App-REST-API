using Domain.Identity;
using Microsoft.AspNetCore.Identity;

namespace Domain.Repositories;

public interface IUserRepository : IRepository, IDisposable
{
    /// <param name="userId"></param>
    /// <returns>The user with the given <paramref name="userId"/></returns>
    Task<User?> GetUserByIdAsync(string userId);

    /// <param name="username"></param>
    /// <returns>The user with the given <paramref name="username"/></returns>
    Task<User?> GetUserByNameAsync(string username);

    /// <param name="userId"></param>
    /// <returns>The username of the user with the given <paramref name="userId"/></returns>
    Task<string> GetUsernameAsync(string userId);

    /// <summary>
    /// Creates a new <paramref name="user"/> with the given input
    /// </summary>
    /// <param name="user"></param>
    /// <returns>User creation result</returns>
    Task<IdentityResult> CreateUserAsync(User user, string password);

    /// <summary>
    /// Updates the user properties
    /// </summary>
    /// <param name="user"></param>
    /// <returns>User update result</returns>
    Task<bool> UpdateUserAsync(User user);

    /// <summary>
    /// Lists registered users that match the given <paramref name="searchString"/>
    /// </summary>
    /// <param name="searchString"></param>
    Task<List<string>> ListUsersAsync(string searchString);
}
