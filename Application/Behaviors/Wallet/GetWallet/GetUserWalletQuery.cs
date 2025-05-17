using Application.Results.Wallet;
using MediatR;

namespace Application.Behaviors.Wallet.GetWallet;

public record GetUserWalletQuery : IRequest<WalletResult>
{
    public string UserId { get; set; } = string.Empty;
}
