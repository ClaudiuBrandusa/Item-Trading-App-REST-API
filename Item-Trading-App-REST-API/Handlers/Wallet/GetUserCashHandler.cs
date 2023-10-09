using Item_Trading_App_REST_API.Requests.Base;
using Item_Trading_App_REST_API.Services.Wallet;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Requests.Wallet;

public class GetUserCashHandler : HandlerBase, IRequestHandler<GetUserCashQuery, int>
{
    public GetUserCashHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public async Task<int> Handle(GetUserCashQuery request, CancellationToken cancellationToken)
    {
        return await Execute<IWalletService, int>(async (walletService) =>
            await walletService.GetUserCashAsync(request.UserId)
        );
    }
}
