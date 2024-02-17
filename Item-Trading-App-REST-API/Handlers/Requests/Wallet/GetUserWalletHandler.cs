using Item_Trading_App_REST_API.Models.Wallet;
using Item_Trading_App_REST_API.Resources.Queries.Wallet;
using Item_Trading_App_REST_API.Services.Wallet;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Requests.Wallet;

public class GetUserWalletHandler : IRequestHandler<GetUserWalletQuery, WalletResult>
{
    private readonly IWalletService _walletService;

    public GetUserWalletHandler(IWalletService walletService)
    {
        _walletService = walletService;
    }

    public Task<WalletResult> Handle(GetUserWalletQuery request, CancellationToken cancellationToken)
    {
        return _walletService.GetWalletAsync(request);
    }
}
