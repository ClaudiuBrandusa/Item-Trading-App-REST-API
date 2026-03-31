using Application.Results.Wallet;
using Application.Services.Wallet;
using MediatR;

namespace Application.Behaviors.Wallet.GetWallet;

public class GetUserWalletHandler : IRequestHandler<GetUserWalletQuery, Result<WalletResult>>
{
    private readonly IWalletService _walletService;

    public GetUserWalletHandler(IWalletService walletService)
    {
        _walletService = walletService;
    }

    public Task<Result<WalletResult>> Handle(GetUserWalletQuery request, CancellationToken cancellationToken)
    {
        return _walletService.GetWalletAsync(request);
    }
}
