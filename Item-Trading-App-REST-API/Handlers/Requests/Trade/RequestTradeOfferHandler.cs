using Item_Trading_App_REST_API.Models.Trade;
using Item_Trading_App_REST_API.Resources.Queries.Trade;
using Item_Trading_App_REST_API.Services.Trade;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Requests.Trade;

public class RequestTradeOfferHandler : IRequestHandler<RequestTradeOfferQuery, TradeOfferResult>
{
    private readonly ITradeService _tradeService;

    public RequestTradeOfferHandler(ITradeService tradeService)
    {
        _tradeService = tradeService;
    }

    public Task<TradeOfferResult> Handle(RequestTradeOfferQuery request, CancellationToken cancellationToken)
    {
        return _tradeService.GetTradeOfferAsync(request);
    }
}
