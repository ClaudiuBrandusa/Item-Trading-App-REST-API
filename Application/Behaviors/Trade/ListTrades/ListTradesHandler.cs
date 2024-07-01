using Application.Models.Trade;
using Application.Services.Trade;
using MediatR;

namespace Application.Behaviors.Trade.ListTrades;

public class ListTradesHandler : IRequestHandler<ListTradesQuery, TradeOffersResult>
{
    private readonly ITradeService _tradeService;

    public ListTradesHandler(ITradeService tradeService)
    {
        _tradeService = tradeService;
    }

    public Task<TradeOffersResult> Handle(ListTradesQuery request, CancellationToken cancellationToken)
    {
        return _tradeService.GetTradeOffersAsync(request);
    }
}
