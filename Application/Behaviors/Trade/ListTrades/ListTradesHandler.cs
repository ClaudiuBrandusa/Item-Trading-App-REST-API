using Application.Results.Trades;
using Application.Services.Trades;
using MediatR;

namespace Application.Behaviors.Trade.ListTrades;

public class ListTradesHandler : IRequestHandler<ListTradesQuery, Result<TradeOffersResult>>
{
    private readonly ITradeService _tradeService;

    public ListTradesHandler(ITradeService tradeService)
    {
        _tradeService = tradeService;
    }

    public Task<Result<TradeOffersResult>> Handle(ListTradesQuery request, CancellationToken cancellationToken)
    {
        return _tradeService.GetTradeOffersAsync(request);
    }
}
