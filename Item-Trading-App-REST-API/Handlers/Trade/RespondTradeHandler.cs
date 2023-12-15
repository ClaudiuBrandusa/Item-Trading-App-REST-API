﻿using Item_Trading_App_REST_API.Models.Trade;
using Item_Trading_App_REST_API.Requests.Base;
using Item_Trading_App_REST_API.Resources.Commands.Trade;
using Item_Trading_App_REST_API.Services.Trade;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Trade;

public class RespondTradeHandler : HandlerBase, IRequestHandler<RespondTradeCommand, RespondedTradeOfferResult>
{
    public RespondTradeHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public Task<RespondedTradeOfferResult> Handle(RespondTradeCommand request, CancellationToken cancellationToken)
    {
        return Execute<ITradeService, RespondedTradeOfferResult>(async tradeService =>
        {
            if (request.Response)
                return await tradeService.AcceptTradeOffer(request);
            else
                return await tradeService.RejectTradeOffer(request);
        });
    }
}