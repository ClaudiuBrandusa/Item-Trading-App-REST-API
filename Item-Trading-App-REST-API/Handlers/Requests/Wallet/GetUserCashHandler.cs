using Item_Trading_App_REST_API.Resources.Queries.Wallet;
using Item_Trading_App_REST_API.Services.Wallet;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Requests.Wallet;

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
