using Item_Trading_App_REST_API.Requests.Base;
using Item_Trading_App_REST_API.Resources.Queries.Trade;
using Item_Trading_App_REST_API.Services.TradeItem;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Trade;

public class ItemUsedInTradeHandler : HandlerBase, IRequestHandler<ItemUsedInTradeQuery, bool>
{
    public ItemUsedInTradeHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public Task<bool> Handle(ItemUsedInTradeQuery request, CancellationToken cancellationToken)
    {
        return Execute<ITradeItemService, bool>(async (tradeService) =>
            (await tradeService.GetItemTradeIdsAsync(request)).Any()
        );
    }
}
