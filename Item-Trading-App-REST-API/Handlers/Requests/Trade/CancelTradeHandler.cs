using Item_Trading_App_REST_API.Handlers.Requests.Base;
using Item_Trading_App_REST_API.Models.Trade;
using Item_Trading_App_REST_API.Resources.Commands.Trade;
using Item_Trading_App_REST_API.Services.Trade;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Requests.Trade;

public class CancelTradeHandler : HandlerBase, IRequestHandler<CancelTradeCommand, TradeOfferResult>
{
    public CancelTradeHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public Task<TradeOfferResult> Handle(CancelTradeCommand request, CancellationToken cancellationToken)
    {
        return Execute<ITradeService, TradeOfferResult>(async tradeService =>
            await tradeService.CancelTradeOfferAsync(request)
        );
    }
}
