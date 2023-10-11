﻿using Item_Trading_App_REST_API.Requests.Base;
using Item_Trading_App_REST_API.Requests.Wallet;
using Item_Trading_App_REST_API.Services.Wallet;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Wallet;

public class TakeCashHandler : HandlerBase, IRequestHandler<TakeCashQuery, bool>
{
    public TakeCashHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public Task<bool> Handle(TakeCashQuery request, CancellationToken cancellationToken)
    {
        return Execute<IWalletService, bool>(async (walletService) =>
            await walletService.TakeCashAsync(request.UserId, request.Amount)
        );
    }
}
