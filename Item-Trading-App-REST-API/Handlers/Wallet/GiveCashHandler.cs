﻿using Item_Trading_App_REST_API.Requests.Base;
using Item_Trading_App_REST_API.Requests.Wallet;
using Item_Trading_App_REST_API.Services.Wallet;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Wallet;

public class GiveCashHandler : HandlerBase, IRequestHandler<GiveCashQuery, bool>
{
    public GiveCashHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public async Task<bool> Handle(GiveCashQuery request, CancellationToken cancellationToken)
    {
        return await Execute<IWalletService, bool>(async (walletService) =>
            await walletService.GiveCashAsync(request.UserId, request.Amount)
        );
    }
}
