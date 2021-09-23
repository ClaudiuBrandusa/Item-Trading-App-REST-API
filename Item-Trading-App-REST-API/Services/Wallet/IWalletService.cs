using Item_Trading_App_REST_API.Models.Wallet;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.Wallet
{
    public interface IWalletService
    {
        Task<WalletResult> GetWalletAsync(string userId);

        Task<WalletResult> UpdateWalletAsync(string userId, int amount);
    }
}
