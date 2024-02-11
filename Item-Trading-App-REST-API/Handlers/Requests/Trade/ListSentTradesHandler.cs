using Item_Trading_App_REST_API.Handlers.Requests.Base;
using Item_Trading_App_REST_API.Models.Trade;
using Item_Trading_App_REST_API.Resources.Queries.Trade;
using Item_Trading_App_REST_API.Services.Trade;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Requests.Trade;

public class ListSentTradesHandler : HandlerBase, IRequestHandler<ListSentTradesQuery, TradeOffersResult>
{
    public ListSentTradesHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public Task<TradeOffersResult> Handle(ListSentTradesQuery request, CancellationToken cancellationToken)
    {
        return Execute<ITradeService, TradeOffersResult>(async tradeService =>
            await tradeService.GetSentTradeOffersAsync(request)
        );
    }
}
