using Item_Trading_App_REST_API.Models.Wallet;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.Wallet;

public interface IWalletService
{
    /// <summary>
    /// Returns the wallet of the given user
    /// </summary>
    Task<WalletResult> GetWalletAsync(string userId);

    /// <summary>
    /// Updates the user's cash value. The amount will overwrite the current cash value.
    /// </summary>
    Task<WalletResult> UpdateWalletAsync(UpdateWallet model);

    /// <summary>
    /// Returns the user's cash value
    /// </summary>
    Task<int> GetUserCashAsync(string userId);

    /// <summary>
    /// Takes the amount from the user's cash value
    /// </summary>
    Task<bool> TakeCashAsync(UpdateWallet model);

    /// <summary>
    /// Gives the amount to the user's cash value
    /// </summary>
    Task<bool> GiveCashAsync(UpdateWallet model);
}
