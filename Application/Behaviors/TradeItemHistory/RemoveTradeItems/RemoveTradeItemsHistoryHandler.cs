using Application.Models.TradeItemHistory;
using Application.Services.TradeItemHistory;
using MediatR;

namespace Application.Behaviors.TradeItemHistory.RemoveTradeItems;

public class RemoveTradeItemsHistoryHandler : IRequestHandler<RemoveTradeItemsHistoryCommand, TradeItemHistoryBaseResult>
{
    private readonly ITradeItemHistoryService _tradeItemHistoryService;

    public RemoveTradeItemsHistoryHandler(ITradeItemHistoryService tradeItemHistoryService)
    {
        _tradeItemHistoryService = tradeItemHistoryService;
    }

    public Task<TradeItemHistoryBaseResult> Handle(RemoveTradeItemsHistoryCommand request, CancellationToken cancellationToken)
    {
        return _tradeItemHistoryService.RemoveTradeItemsAsync(request);
    }
}
