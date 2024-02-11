using Item_Trading_App_REST_API.Handlers.Requests.Base;
using Item_Trading_App_REST_API.Resources.Queries.Item;
using Item_Trading_App_REST_API.Services.TradeItem;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Requests.Item;

public class GetTradesUsingTheItemHandler : HandlerBase, IRequestHandler<GetTradesUsingTheItemQuery, string[]>
{
    public GetTradesUsingTheItemHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public Task<string[]> Handle(GetTradesUsingTheItemQuery request, CancellationToken cancellationToken)
    {
        return Execute<ITradeItemService, string[]>(async (tradeService) =>
            await tradeService.GetItemTradeIdsAsync(request)
        );
    }
}
