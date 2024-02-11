using Item_Trading_App_REST_API.Handlers.Requests.Base;
using Item_Trading_App_REST_API.Resources.Commands.TradeItem;
using Item_Trading_App_REST_API.Services.TradeItem;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Requests.TradeItem;

public class RemoveTradeItemsHandler : HandlerBase, IRequestHandler<RemoveTradeItemsCommand, bool>
{
    public RemoveTradeItemsHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public Task<bool> Handle(RemoveTradeItemsCommand request, CancellationToken cancellationToken)
    {
        return Execute(async (ITradeItemService tradeItemService) =>
            await tradeItemService.RemoveTradeItemsAsync(request)
        );
    }
}
