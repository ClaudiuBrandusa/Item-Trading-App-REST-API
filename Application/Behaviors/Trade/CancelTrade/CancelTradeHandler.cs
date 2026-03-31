using Application.Results.Trades;
using Application.Services.Trades;
using MediatR;

namespace Application.Behaviors.Trade.CancelTrade;

public class CancelTradeHandler : IRequestHandler<CancelTradeCommand, Result<TradeOfferResult>>
{
    private readonly ITradeService _tradeService;

    public CancelTradeHandler(ITradeService tradeService)
    {
        _tradeService = tradeService;
    }

    public Task<Result<TradeOfferResult>> Handle(CancelTradeCommand request, CancellationToken cancellationToken)
    {
        return _tradeService.CancelTradeOfferAsync(request);
    }
}
