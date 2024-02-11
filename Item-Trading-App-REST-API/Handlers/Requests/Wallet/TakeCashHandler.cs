using Item_Trading_App_REST_API.Handlers.Requests.Base;
using Item_Trading_App_REST_API.Resources.Commands.Wallet;
using Item_Trading_App_REST_API.Services.Wallet;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Requests.Wallet;

public class TakeCashHandler : HandlerBase, IRequestHandler<TakeCashCommand, bool>
{
    public TakeCashHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public Task<bool> Handle(TakeCashCommand request, CancellationToken cancellationToken)
    {
        return Execute<IWalletService, bool>(async (walletService) =>
            await walletService.TakeCashAsync(request)
        );
    }
}
