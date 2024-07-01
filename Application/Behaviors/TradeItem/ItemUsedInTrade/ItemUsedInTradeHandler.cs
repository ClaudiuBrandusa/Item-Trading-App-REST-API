using Application.Behaviors.TradeItem.GetTradeItemIds;
using Application.Extensions;
using Application.Services.TradeItem;
using MapsterMapper;
using MediatR;

namespace Application.Behaviors.TradeItem.ItemUsedInTrade;

public class ItemUsedInTradeHandler : IRequestHandler<ItemUsedInTradeQuery, bool>
{
    private readonly ITradeItemService _tradeItemService;
    private readonly IMapper _mapper;

    public ItemUsedInTradeHandler(ITradeItemService tradeItemService, IMapper mapper)
    {
        _tradeItemService = tradeItemService;
        _mapper = mapper;
    }

    public async Task<bool> Handle(ItemUsedInTradeQuery request, CancellationToken cancellationToken)
    {
        return (await _tradeItemService.GetItemTradeIdsAsync(_mapper.AdaptToType<ItemUsedInTradeQuery, GetTradesUsingTheItemQuery>(request))).Any();
    }
}
