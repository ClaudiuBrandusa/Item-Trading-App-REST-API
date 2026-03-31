using Application.Behaviors.Identity.GetUsername;
using Application.Behaviors.Identity.ListUsers;
using Application.Behaviors.Identity.LoginUser;
using Application.Behaviors.Identity.RefreshToken;
using Application.Behaviors.Identity.RegisterUser;
using Application.Results.Identity;

namespace Application.Services.Identity;

public interface IIdentityService
{
    /// <summary>
    /// Registers the user only if the input data is valid
    /// </summary>
    Task<Result<AuthenticationResult>> RegisterAsync(RegisterCommand model);

    /// <summary>
    /// Connects the user if the input data matches with a registered account
    /// </summary>
    Task<Result<AuthenticationResult>> LoginAsync(LoginCommand model);

    /// <summary>
    /// Refreshes the user's token only if it has expired
    /// </summary>
    Task<Result<AuthenticationResult>> RefreshTokenAsync(RefreshTokenCommand model);

    /// <summary>
    /// Returns the username with the given user id
    /// </summary>
    Task<string> GetUsername(GetUsernameQuery model);

    /// <summary>
    /// Returns a list with all of the users besides the user requesting the service
    /// </summary>
    Task<Result<UsersResult>> ListUsers(ListUsersQuery model);
}
