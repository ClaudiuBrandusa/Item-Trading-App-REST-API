using Item_Trading_App_REST_API.Handlers.Requests.Base;
using Item_Trading_App_REST_API.Models.Wallet;
using Item_Trading_App_REST_API.Resources.Commands.Wallet;
using Item_Trading_App_REST_API.Services.Wallet;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Requests.Wallet;

public class UpdateWalletHandler : HandlerBase, IRequestHandler<UpdateWalletCommand, WalletResult>
{
    public UpdateWalletHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public Task<WalletResult> Handle(UpdateWalletCommand request, CancellationToken cancellationToken)
    {
        return Execute<IWalletService, WalletResult>(async (walletService) =>
            await walletService.UpdateWalletAsync(request)
        );
    }
}
