using Application.Results.Wallet;
using MediatR;

namespace Application.Behaviors.Wallet.UpdateWallet;

public record UpdateWalletCommand : IRequest<Result<WalletResult>>
{
    public string UserId { get; set; } = string.Empty;

    public int Quantity { get; set; }
}
