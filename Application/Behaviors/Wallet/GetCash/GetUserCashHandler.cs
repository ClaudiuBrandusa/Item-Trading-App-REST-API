using Application.Services.Wallet;
using MediatR;

namespace Application.Behaviors.Wallet.GetCash;

public class GetUserCashHandler : IRequestHandler<GetUserCashQuery, int>
{
    private readonly IWalletService _walletService;

    public GetUserCashHandler(IWalletService walletService)
    {
        _walletService = walletService;
    }

    public Task<int> Handle(GetUserCashQuery request, CancellationToken cancellationToken)
    {
        return _walletService.GetUserCashAsync(request);
    }
}
