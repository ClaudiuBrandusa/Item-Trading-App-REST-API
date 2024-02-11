using Item_Trading_App_REST_API.Handlers.Requests.Base;
using Item_Trading_App_REST_API.Models.Trade;
using Item_Trading_App_REST_API.Resources.Queries.Trade;
using Item_Trading_App_REST_API.Services.Trade;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Requests.Trade;

public class ListReceivedTradesHandler : HandlerBase, IRequestHandler<ListReceivedTradesQuery, TradeOffersResult>
{
    public ListReceivedTradesHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public Task<TradeOffersResult> Handle(ListReceivedTradesQuery request, CancellationToken cancellationToken)
    {
        return Execute<ITradeService, TradeOffersResult>(async tradeService =>
            await tradeService.GetReceivedTradeOffersAsync(request)
        );
    }
}
