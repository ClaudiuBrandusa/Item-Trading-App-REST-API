using Item_Trading_App_REST_API.Handlers.Requests.Base;
using Item_Trading_App_REST_API.Resources.Queries.TradeItem;
using Item_Trading_App_REST_API.Services.TradeItem;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Requests.TradeItem;

public class GetTradeItemsHandler : HandlerBase, IRequestHandler<GetTradeItemsQuery, Models.TradeItems.TradeItem[]>
{
    public GetTradeItemsHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public Task<Models.TradeItems.TradeItem[]> Handle(GetTradeItemsQuery request, CancellationToken cancellationToken)
    {
        return Execute(async (ITradeItemService tradeItemService) =>
            await tradeItemService.GetTradeItemsAsync(request)
        );
    }
}
