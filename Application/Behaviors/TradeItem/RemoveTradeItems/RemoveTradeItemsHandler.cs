using Application.Services.TradeItem;
using MediatR;

namespace Application.Behaviors.TradeItem.RemoveTradeItems;

public class RemoveTradeItemsHandler : IRequestHandler<RemoveTradeItemsCommand, bool>
{
    private readonly ITradeItemService _tradeItemService;

    public RemoveTradeItemsHandler(ITradeItemService tradeItemService)
    {
        _tradeItemService = tradeItemService;
    }

    public Task<bool> Handle(RemoveTradeItemsCommand request, CancellationToken cancellationToken)
    {
        return _tradeItemService.RemoveTradeItemsAsync(request);
    }
}
