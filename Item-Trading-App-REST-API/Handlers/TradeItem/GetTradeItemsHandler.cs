using Item_Trading_App_REST_API.Requests.Base;
using Item_Trading_App_REST_API.Resources.Queries.TradeItem;
using Item_Trading_App_REST_API.Services.TradeItem;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.TradeItem;

public class GetTradeItemsHandler : HandlerBase, IRequestHandler<GetTradeItemsQuery, List<Models.TradeItems.TradeItem>>
{
    public GetTradeItemsHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public Task<List<Models.TradeItems.TradeItem>> Handle(GetTradeItemsQuery request, CancellationToken cancellationToken)
    {
        return Execute<ITradeItemService, List<Models.TradeItems.TradeItem>>(async tradeItemService =>
            await tradeItemService.GetTradeItemsAsync(request)
        );
    }
}
