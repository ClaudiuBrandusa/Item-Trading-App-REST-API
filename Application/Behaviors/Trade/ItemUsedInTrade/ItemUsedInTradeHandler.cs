using Application.Services.Trades;
using MediatR;

namespace Application.Behaviors.Trade.ItemUsedInTrade;

public class ItemUsedInTradeHandler : IRequestHandler<ItemUsedInTradeQuery, bool>
{
    private readonly ITradeService _tradeService;

    public ItemUsedInTradeHandler(ITradeService tradeService)
    {
        _tradeService = tradeService;
    }

    public async Task<bool> Handle(ItemUsedInTradeQuery request, CancellationToken cancellationToken)
    {
        return await _tradeService.IsItemUsedInTrade(request);
    }
}
