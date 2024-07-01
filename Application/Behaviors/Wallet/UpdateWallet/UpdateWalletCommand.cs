using Application.Models.Wallet;
using MediatR;

namespace Application.Behaviors.Wallet.UpdateWallet;

public record UpdateWalletCommand : IRequest<WalletResult>
{
    public string UserId { get; set; }

    public int Quantity { get; set; }
}
