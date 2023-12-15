using Item_Trading_App_REST_API.Models.Trade;
using Item_Trading_App_REST_API.Requests.Base;
using Item_Trading_App_REST_API.Resources.Queries.Trade;
using Item_Trading_App_REST_API.Services.Trade;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Trade;

public class ListRespondedSentTradesHandler : HandlerBase, IRequestHandler<ListRespondedSentTradesQuery, TradeOffersResult>
{
    public ListRespondedSentTradesHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public Task<TradeOffersResult> Handle(ListRespondedSentTradesQuery request, CancellationToken cancellationToken)
    {
        return Execute<ITradeService, TradeOffersResult>(async tradeService =>
            await tradeService.GetSentRespondedTradeOffers(request)
        );
    }
}
