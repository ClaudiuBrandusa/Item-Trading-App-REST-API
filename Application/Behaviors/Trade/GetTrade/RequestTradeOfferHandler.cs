using Application.Results.Trades;
using Application.Services.Trades;
using MediatR;

namespace Application.Behaviors.Trade.GetTrade;

public class RequestTradeOfferHandler : IRequestHandler<RequestTradeOfferQuery, Result<TradeOfferResult>>
{
    private readonly ITradeService _tradeService;

    public RequestTradeOfferHandler(ITradeService tradeService)
    {
        _tradeService = tradeService;
    }

    public Task<Result<TradeOfferResult>> Handle(RequestTradeOfferQuery request, CancellationToken cancellationToken)
    {
        return _tradeService.GetTradeOfferAsync(request);
    }
}
