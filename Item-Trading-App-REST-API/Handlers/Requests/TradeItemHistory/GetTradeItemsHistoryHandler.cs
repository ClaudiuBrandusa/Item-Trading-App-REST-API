using Item_Trading_App_REST_API.Handlers.Requests.Base;
using Item_Trading_App_REST_API.Resources.Queries.TradeItemHistory;
using Item_Trading_App_REST_API.Services.TradeItemHistory;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Requests.TradeItemHistory;

public class GetTradeItemsHistoryHandler : HandlerBase, IRequestHandler<GetTradeItemsHistoryQuery, Models.TradeItems.TradeItem[]>
{
    public GetTradeItemsHistoryHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public Task<Models.TradeItems.TradeItem[]> Handle(GetTradeItemsHistoryQuery request, CancellationToken cancellationToken)
    {
        return Execute(async (ITradeItemHistoryService tradeItemHistoryService) =>
            await tradeItemHistoryService.GetTradeItemsAsync(request)
        );
    }
}
