using Application.Services.Wallet;
using MediatR;

namespace Application.Behaviors.Wallet.TakeCash;

public class TakeCashHandler : IRequestHandler<TakeCashCommand, bool>
{
    private readonly IWalletService _walletService;

    public TakeCashHandler(IWalletService walletService)
    {
        _walletService = walletService;
    }

    public Task<bool> Handle(TakeCashCommand request, CancellationToken cancellationToken)
    {
        return _walletService.TakeCashAsync(request);
    }
}
