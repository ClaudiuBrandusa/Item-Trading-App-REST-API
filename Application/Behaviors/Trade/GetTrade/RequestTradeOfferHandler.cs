using Application.Models.Trade;
using Application.Services.Trade;
using MediatR;

namespace Application.Behaviors.Trade.GetTrade;

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
