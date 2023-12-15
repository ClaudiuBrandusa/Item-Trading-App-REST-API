﻿using Item_Trading_App_REST_API.Requests.Base;
using Item_Trading_App_REST_API.Resources.Commands.Wallet;
using Item_Trading_App_REST_API.Services.Wallet;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Wallet;

public class GiveCashHandler : HandlerBase, IRequestHandler<GiveCashCommand, bool>
{
    public GiveCashHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public Task<bool> Handle(GiveCashCommand request, CancellationToken cancellationToken)
    {
        return Execute<IWalletService, bool>(async (walletService) =>
            await walletService.GiveCashAsync(request)
        );
    }
}
