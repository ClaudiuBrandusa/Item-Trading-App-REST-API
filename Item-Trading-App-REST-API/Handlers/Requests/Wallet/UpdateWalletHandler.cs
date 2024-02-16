using Item_Trading_App_REST_API.Models.Wallet;
using Item_Trading_App_REST_API.Resources.Commands.Wallet;
using Item_Trading_App_REST_API.Services.Wallet;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Requests.Wallet;

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
