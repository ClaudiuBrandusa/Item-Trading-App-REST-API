using Application.Services.TradeItem;
using MediatR;

namespace Application.Behaviors.TradeItem.GetTradeItems;

public class GetTradeItemsHandler : IRequestHandler<GetTradeItemsQuery, Domain.TradeItems.TradeItem[]>
{
    private readonly ITradeItemService _tradeItemService;

    public GetTradeItemsHandler(ITradeItemService tradeItemService)
    {
        _tradeItemService = tradeItemService;
    }

    public Task<Domain.TradeItems.TradeItem[]> Handle(GetTradeItemsQuery request, CancellationToken cancellationToken)
    {
        return _tradeItemService.GetTradeItemsAsync(request);
    }
}
