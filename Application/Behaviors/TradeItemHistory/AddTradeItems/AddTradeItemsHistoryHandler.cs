using Application.Models.TradeItemHistory;
using Application.Services.TradeItemHistory;
using MediatR;

namespace Application.Behaviors.TradeItemHistory.AddTradeItems;

public class AddTradeItemsHistoryHandler : IRequestHandler<AddTradeItemsHistoryCommand, TradeItemHistoryBaseResult>
{
    private readonly ITradeItemHistoryService _tradeItemHistoryService;

    public AddTradeItemsHistoryHandler(ITradeItemHistoryService tradeItemHistoryService)
    {
        _tradeItemHistoryService = tradeItemHistoryService;
    }

    public Task<TradeItemHistoryBaseResult> Handle(AddTradeItemsHistoryCommand request, CancellationToken cancellationToken)
    {
        return _tradeItemHistoryService.AddTradeItemsAsync(request);
    }
}
