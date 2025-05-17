using Application.Results.TradeItemsHistory;
using Application.Services.TradeItemsHistory;
using MediatR;

namespace Application.Behaviors.TradeItemHistory.RemoveTradeItems;

public class RemoveTradeItemsHistoryHandler : IRequestHandler<RemoveTradeItemsHistoryCommand, TradeItemHistoryResult>
{
    private readonly ITradeItemHistoryService _tradeItemHistoryService;

    public RemoveTradeItemsHistoryHandler(ITradeItemHistoryService tradeItemHistoryService)
    {
        _tradeItemHistoryService = tradeItemHistoryService;
    }

    public Task<TradeItemHistoryResult> Handle(RemoveTradeItemsHistoryCommand request, CancellationToken cancellationToken)
    {
        return _tradeItemHistoryService.RemoveTradeItemsAsync(request);
    }
}
