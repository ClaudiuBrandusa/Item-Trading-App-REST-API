using Item_Trading_App_REST_API.Requests.Base;
using Item_Trading_App_REST_API.Requests.TradeItem;
using Item_Trading_App_REST_API.Services.TradeItem;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.TradeContent;

public class GetTradeItemsHandler : HandlerBase, IRequestHandler<GetTradeItemsQuery, List<Models.Trade.TradeItem>>
{
    public GetTradeItemsHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public Task<List<Models.Trade.TradeItem>> Handle(GetTradeItemsQuery request, CancellationToken cancellationToken)
    {
        return Execute<ITradeItemService, List<Models.Trade.TradeItem>>(async tradeItemService =>
            await tradeItemService.GetTradeItemsAsync(request.TradeId)
        );
    }
}
