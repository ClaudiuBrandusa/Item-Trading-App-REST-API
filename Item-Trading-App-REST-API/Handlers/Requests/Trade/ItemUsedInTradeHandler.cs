using Item_Trading_App_REST_API.Extensions;
using Item_Trading_App_REST_API.Resources.Queries.Item;
using Item_Trading_App_REST_API.Resources.Queries.Trade;
using Item_Trading_App_REST_API.Services.TradeItem;
using MapsterMapper;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Requests.Trade;

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
