using Application.Services.TradeItemHistory;
using MediatR;

namespace Application.Behaviors.TradeItemHistory.GetTradeItems;

public class GetTradeItemsHistoryHandler : IRequestHandler<GetTradeItemsHistoryQuery, Domain.TradeItems.TradeItem[]>
{
    private readonly ITradeItemHistoryService _tradeItemHistoryService;

    public GetTradeItemsHistoryHandler(ITradeItemHistoryService tradeItemHistoryService)
    {
        _tradeItemHistoryService = tradeItemHistoryService;
    }

    public Task<Domain.TradeItems.TradeItem[]> Handle(GetTradeItemsHistoryQuery request, CancellationToken cancellationToken)
    {
        return _tradeItemHistoryService.GetTradeItemsAsync(request);
    }
}
