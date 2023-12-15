using Item_Trading_App_REST_API.Models.Identity;
using Item_Trading_App_REST_API.Resources.Commands.Identity;
using Item_Trading_App_REST_API.Resources.Queries.Identity;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.Identity;

public interface IIdentityService
{
    /// <summary>
    /// Registers the user only if the input data is valid
    /// </summary>
    Task<AuthenticationResult> RegisterAsync(RegisterCommand model);

    /// <summary>
    /// Connects the user if the input data matches with a registered account
    /// </summary>
    Task<AuthenticationResult> LoginAsync(LoginCommand model);

    /// <summary>
    /// Refreshes the user's token only if it has expired
    /// </summary>
    Task<AuthenticationResult> RefreshTokenAsync(RefreshTokenCommand model);

    /// <summary>
    /// Returns the username with the given user id
    /// </summary>
    Task<string> GetUsername(GetUsernameQuery model);

    /// <summary>
    /// Returns a list with all of the users besides the user requesting the service
    /// </summary>
    Task<UsersResult> ListUsers(ListUsersQuery model);
}
