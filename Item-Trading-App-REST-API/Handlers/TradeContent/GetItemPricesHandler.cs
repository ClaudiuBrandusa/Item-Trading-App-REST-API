using Item_Trading_App_REST_API.Models.Item;
using Item_Trading_App_REST_API.Requests.Base;
using Item_Trading_App_REST_API.Requests.TradeItem;
using Item_Trading_App_REST_API.Services.TradeItem;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.TradeContent;

public class GetItemPricesHandler : HandlerBase, IRequestHandler<GetItemPricesQuery, List<ItemPrice>>
{
    public GetItemPricesHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public Task<List<ItemPrice>> Handle(GetItemPricesQuery request, CancellationToken cancellationToken)
    {
        return Execute<ITradeItemService, List<ItemPrice>>(async tradeContentService =>
            await tradeContentService.GetItemPricesAsync(request)
        );
    }
}
