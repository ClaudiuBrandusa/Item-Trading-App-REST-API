using Item_Trading_App_REST_API.Requests.Base;
using Item_Trading_App_REST_API.Resources.Queries.TradeItem;
using Item_Trading_App_REST_API.Services.TradeItem;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.TradeItem;

public class HasTradeItemHandler : HandlerBase, IRequestHandler<HasTradeItemQuery, bool>
{
    public HasTradeItemHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public Task<bool> Handle(HasTradeItemQuery request, CancellationToken cancellationToken)
    {
        return Execute(async (ITradeItemService tradeItemService) =>
            await tradeItemService.HasTradeItemAsync(request)
        );
    }
}
