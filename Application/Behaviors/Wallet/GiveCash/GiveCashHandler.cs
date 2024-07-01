using Application.Services.Wallet;
using MediatR;

namespace Application.Behaviors.Wallet.GiveCash;

public class GiveCashHandler : IRequestHandler<GiveCashCommand, bool>
{
    private readonly IWalletService _walletService;

    public GiveCashHandler(IWalletService walletService)
    {
        _walletService = walletService;
    }

    public Task<bool> Handle(GiveCashCommand request, CancellationToken cancellationToken)
    {
        return _walletService.GiveCashAsync(request);
    }
}
