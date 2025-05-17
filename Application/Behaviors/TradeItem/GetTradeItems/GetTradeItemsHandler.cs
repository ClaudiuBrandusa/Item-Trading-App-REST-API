using Application.Services.TradeItems;
using MediatR;

namespace Application.Behaviors.TradeItem.GetTradeItems;

public class GetTradeItemsHandler : IRequestHandler<GetTradeItemsQuery, Domain.Entities.Trades.TradeItem[]>
{
    private readonly ITradeItemService _tradeItemService;

    public GetTradeItemsHandler(ITradeItemService tradeItemService)
    {
        _tradeItemService = tradeItemService;
    }

    public Task<Domain.Entities.Trades.TradeItem[]> Handle(GetTradeItemsQuery request, CancellationToken cancellationToken)
    {
        return _tradeItemService.GetTradeItemAsync(request);
    }
}
