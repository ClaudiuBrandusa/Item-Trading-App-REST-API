using Item_Trading_App_REST_API.Models.Trade;
using Item_Trading_App_REST_API.Resources.Queries.Trade;
using Item_Trading_App_REST_API.Services.Trade;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Requests.Trade;

public class ListSentTradesHandler : IRequestHandler<ListSentTradesQuery, TradeOffersResult>
{
    private readonly ITradeService _tradeService;

    public ListSentTradesHandler(ITradeService tradeService)
    {
        _tradeService = tradeService;
    }

    public Task<TradeOffersResult> Handle(ListSentTradesQuery request, CancellationToken cancellationToken)
    {
        return _tradeService.GetSentTradeOffersAsync(request);
    }
}
