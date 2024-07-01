using Application.Services.TradeItem;
using MediatR;

namespace Application.Behaviors.TradeItem.GetTradeItemIds;

public class GetTradesUsingTheItemHandler : IRequestHandler<GetTradesUsingTheItemQuery, string[]>
{
    private readonly ITradeItemService _tradeItemService;

    public GetTradesUsingTheItemHandler(ITradeItemService tradeItemService)
    {
        _tradeItemService = tradeItemService;
    }

    public Task<string[]> Handle(GetTradesUsingTheItemQuery request, CancellationToken cancellationToken)
    {
        return _tradeItemService.GetItemTradeIdsAsync(request);
    }
}
