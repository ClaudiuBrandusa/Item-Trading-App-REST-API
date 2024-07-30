using Application.Results.Trades;
using Application.Services.Trade;
using MediatR;

namespace Application.Behaviors.Trade.CancelTrade;

public class CancelTradeHandler : IRequestHandler<CancelTradeCommand, TradeOfferResult>
{
    private readonly ITradeService _tradeService;

    public CancelTradeHandler(ITradeService tradeService)
    {
        _tradeService = tradeService;
    }

    public Task<TradeOfferResult> Handle(CancelTradeCommand request, CancellationToken cancellationToken)
    {
        return _tradeService.CancelTradeOfferAsync(request);
    }
}
