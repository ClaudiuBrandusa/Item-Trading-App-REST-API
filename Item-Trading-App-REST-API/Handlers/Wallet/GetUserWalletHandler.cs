using Item_Trading_App_REST_API.Models.Wallet;
using Item_Trading_App_REST_API.Requests.Base;
using Item_Trading_App_REST_API.Resources.Queries.Wallet;
using Item_Trading_App_REST_API.Services.Wallet;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Wallet;

public class GetUserWalletHandler : HandlerBase, IRequestHandler<GetUserWalletQuery, WalletResult>
{
    public GetUserWalletHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public Task<WalletResult> Handle(GetUserWalletQuery request, CancellationToken cancellationToken)
    {
        return Execute<IWalletService, WalletResult>(async (walletService) =>
            await walletService.GetWalletAsync(request)
        );
    }
}
