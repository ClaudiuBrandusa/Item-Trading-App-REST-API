using Application.Results.Wallet;
using Application.Services.Wallet;
using MediatR;

namespace Application.Behaviors.Wallet.UpdateWallet;

public class UpdateWalletHandler : IRequestHandler<UpdateWalletCommand, WalletResult>
{
    private readonly IWalletService _walletService;

    public UpdateWalletHandler(IWalletService walletService)
    {
        _walletService = walletService;
    }

    public Task<WalletResult> Handle(UpdateWalletCommand request, CancellationToken cancellationToken)
    {
        return _walletService.UpdateWalletAsync(request);
    }
}
