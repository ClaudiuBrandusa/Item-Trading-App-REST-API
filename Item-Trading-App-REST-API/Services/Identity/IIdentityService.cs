using Item_Trading_App_REST_API.Models.Identity;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.Identity
{
    public interface IIdentityService
    {
        /// <summary>
        /// Registers the user only if the input data is valid
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        Task<AuthenticationResult> RegisterAsync(string username, string password);

        /// <summary>
        /// Connects the user if the input data matches with a registered account
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        Task<AuthenticationResult> LoginAsync(string username, string password);

        /// <summary>
        /// Refreshes the user's token only if it has expired
        /// </summary>
        /// <param name="token"></param>
        /// <param name="refreshToken"></param>
        /// <returns></returns>
        Task<AuthenticationResult> RefreshTokenAsync(string token, string refreshToken);

        /// <summary>
        /// Returns the username with the given user id
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<string> GetUsername(string userId);

        /// <summary>
        /// Returns a list with all of the users besides the user requesting the service
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="searchString"></param>
        /// <returns></returns>
        Task<UsersResult> ListUsers(string userId, string searchString);
    }
}
