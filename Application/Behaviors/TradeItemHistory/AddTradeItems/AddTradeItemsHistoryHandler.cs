using Application.Results.TradeItemsHistory;
using Application.Services.TradeItemsHistory;
using MediatR;

namespace Application.Behaviors.TradeItemHistory.AddTradeItems;

public class AddTradeItemsHistoryHandler : IRequestHandler<AddTradeItemsHistoryCommand, TradeItemHistoryResult>
{
    private readonly ITradeItemHistoryService _tradeItemHistoryService;

    public AddTradeItemsHistoryHandler(ITradeItemHistoryService tradeItemHistoryService)
    {
        _tradeItemHistoryService = tradeItemHistoryService;
    }

    public Task<TradeItemHistoryResult> Handle(AddTradeItemsHistoryCommand request, CancellationToken cancellationToken)
    {
        return _tradeItemHistoryService.AddTradeItemsAsync(request);
    }
}
