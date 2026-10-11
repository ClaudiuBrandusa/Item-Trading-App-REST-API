using Application.Behaviors.Wallet.GetCash;
using Application.Behaviors.Wallet.GetWallet;
using Application.Behaviors.Wallet.GiveCash;
using Application.Behaviors.Wallet.TakeCash;
using Application.Behaviors.Wallet.UpdateWallet;
using Application.Results.Wallet;

namespace Application.Services.Wallet;

public interface IWalletService
{
    /// <summary>
    /// Returns the wallet of the given user
    /// </summary>
    Task<Result<WalletResult>> GetWalletAsync(GetUserWalletQuery model, CancellationToken cancellationToken);

    /// <summary>
    /// Updates the user's cash value. The amount will overwrite the current cash value.
    /// </summary>
    Task<Result<WalletResult>> UpdateWalletAsync(UpdateWalletCommand model, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the user's cash value
    /// </summary>
    Task<int> GetUserCashAsync(GetUserCashQuery model, CancellationToken cancellationToken);

    /// <summary>
    /// Takes the amount from the user's cash value
    /// </summary>
    Task<bool> TakeCashAsync(TakeCashCommand model, CancellationToken cancellationToken);

    /// <summary>
    /// Gives the amount to the user's cash value
    /// </summary>
    Task<bool> GiveCashAsync(GiveCashCommand model, CancellationToken cancellationToken);
}
