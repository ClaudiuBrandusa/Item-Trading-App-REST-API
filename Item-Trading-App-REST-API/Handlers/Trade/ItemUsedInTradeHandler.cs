using Item_Trading_App_REST_API.Extensions;
using Item_Trading_App_REST_API.Requests.Base;
using Item_Trading_App_REST_API.Resources.Queries.Item;
using Item_Trading_App_REST_API.Resources.Queries.Trade;
using Item_Trading_App_REST_API.Services.TradeItem;
using MapsterMapper;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Trade;

public class ItemUsedInTradeHandler : HandlerBase, IRequestHandler<ItemUsedInTradeQuery, bool>
{
    private readonly IMapper _mapper;

    public ItemUsedInTradeHandler(IServiceProvider serviceProvider, IMapper mapper) : base(serviceProvider)
    {
        _mapper = mapper;
    }

    public Task<bool> Handle(ItemUsedInTradeQuery request, CancellationToken cancellationToken)
    {
        return Execute<ITradeItemService, bool>(async (tradeItemService) =>
            (await tradeItemService.GetItemTradeIdsAsync(_mapper.AdaptToType<ItemUsedInTradeQuery, GetTradesUsingTheItemQuery>(request))).Any()
        );
    }
}
