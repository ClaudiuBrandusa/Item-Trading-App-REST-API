using Item_Trading_App_REST_API.Resources.Commands.Wallet;
using Item_Trading_App_REST_API.Services.Wallet;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Requests.Wallet;

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
