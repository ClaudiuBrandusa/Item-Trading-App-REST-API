using Item_Trading_App_REST_API.Models.Identity;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.Identity
{
    public interface IIdentityService
    {
        Task<AuthenticationResult> RegisterAsync(string username, string password);

        Task<AuthenticationResult> LoginAsync(string username, string password);

        Task<AuthenticationResult> RefreshTokenAsync(string token, string refreshToken);
    }
}
