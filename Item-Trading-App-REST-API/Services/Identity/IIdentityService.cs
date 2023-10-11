using Item_Trading_App_REST_API.Models.Identity;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.Identity;

public interface IIdentityService
{
    /// <summary>
    /// Registers the user only if the input data is valid
    /// </summary>
    Task<AuthenticationResult> RegisterAsync(string username, string password);

    /// <summary>
    /// Connects the user if the input data matches with a registered account
    /// </summary>
    Task<AuthenticationResult> LoginAsync(string username, string password);

    /// <summary>
    /// Refreshes the user's token only if it has expired
    /// </summary>
    Task<AuthenticationResult> RefreshTokenAsync(string token, string refreshToken);

    /// <summary>
    /// Returns the username with the given user id
    /// </summary>
    Task<string> GetUsername(string userId);

    /// <summary>
    /// Returns a list with all of the users besides the user requesting the service
    /// </summary>
    Task<UsersResult> ListUsers(string userId, string searchString);
}
