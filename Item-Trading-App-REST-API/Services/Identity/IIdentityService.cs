using Item_Trading_App_REST_API.Models.Identity;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.Identity;

public interface IIdentityService
{
    /// <summary>
    /// Registers the user only if the input data is valid
    /// </summary>
    Task<AuthenticationResult> RegisterAsync(Register model);

    /// <summary>
    /// Connects the user if the input data matches with a registered account
    /// </summary>
    Task<AuthenticationResult> LoginAsync(Login model);

    /// <summary>
    /// Refreshes the user's token only if it has expired
    /// </summary>
    Task<AuthenticationResult> RefreshTokenAsync(RefreshTokenData model);

    /// <summary>
    /// Returns the username with the given user id
    /// </summary>
    Task<string> GetUsername(string userId);

    /// <summary>
    /// Returns a list with all of the users besides the user requesting the service
    /// </summary>
    Task<UsersResult> ListUsers(ListUsers model);
}
