using Application.Services.TradeItems;
using MediatR;

namespace Application.Behaviors.TradeItem.HasTradeItem;

public class HasTradeItemHandler : IRequestHandler<HasTradeItemQuery, bool>
{
    private readonly ITradeItemService _tradeItemService;

    public HasTradeItemHandler(ITradeItemService tradeItemService)
    {
        _tradeItemService = tradeItemService;
    }

    public Task<bool> Handle(HasTradeItemQuery request, CancellationToken cancellationToken)
    {
        return _tradeItemService.HasTradeItemAsync(request);
    }
}
