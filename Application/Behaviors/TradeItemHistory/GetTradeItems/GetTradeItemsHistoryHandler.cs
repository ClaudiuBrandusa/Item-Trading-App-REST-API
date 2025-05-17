using Application.Services.TradeItemsHistory;
using MediatR;

namespace Application.Behaviors.TradeItemHistory.GetTradeItems;

public class GetTradeItemsHistoryHandler : IRequestHandler<GetTradeItemsHistoryQuery, Domain.Entities.Trades.TradeItem[]>
{
    private readonly ITradeItemHistoryService _tradeItemHistoryService;

    public GetTradeItemsHistoryHandler(ITradeItemHistoryService tradeItemHistoryService)
    {
        _tradeItemHistoryService = tradeItemHistoryService;
    }

    public Task<Domain.Entities.Trades.TradeItem[]> Handle(GetTradeItemsHistoryQuery request, CancellationToken cancellationToken)
    {
        return _tradeItemHistoryService.GetTradeItemsAsync(request);
    }
}
