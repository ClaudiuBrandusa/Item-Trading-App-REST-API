using Item_Trading_App_REST_API.Models.Wallet;
using Item_Trading_App_REST_API.Resources.Commands.Wallet;
using Item_Trading_App_REST_API.Resources.Queries.Wallet;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.Wallet;

public interface IWalletService
{
    /// <summary>
    /// Returns the wallet of the given user
    /// </summary>
    Task<WalletResult> GetWalletAsync(GetUserWalletQuery model);

    /// <summary>
    /// Updates the user's cash value. The amount will overwrite the current cash value.
    /// </summary>
    Task<WalletResult> UpdateWalletAsync(UpdateWalletCommand model);

    /// <summary>
    /// Returns the user's cash value
    /// </summary>
    Task<int> GetUserCashAsync(GetUserCashQuery model);

    /// <summary>
    /// Takes the amount from the user's cash value
    /// </summary>
    Task<bool> TakeCashAsync(TakeCashCommand model);

    /// <summary>
    /// Gives the amount to the user's cash value
    /// </summary>
    Task<bool> GiveCashAsync(GiveCashCommand model);
}
