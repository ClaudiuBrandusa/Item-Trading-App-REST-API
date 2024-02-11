using Item_Trading_App_REST_API.Handlers.Requests.Base;
using Item_Trading_App_REST_API.Models.TradeItemHistory;
using Item_Trading_App_REST_API.Resources.Commands.TradeItemHistory;
using Item_Trading_App_REST_API.Services.TradeItemHistory;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Requests.TradeItemHistory;

public class RemoveTradeItemsHistoryHandler : HandlerBase, IRequestHandler<RemoveTradeItemsHistoryCommand, TradeItemHistoryBaseResult>
{
    public RemoveTradeItemsHistoryHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public Task<TradeItemHistoryBaseResult> Handle(RemoveTradeItemsHistoryCommand request, CancellationToken cancellationToken)
    {
        return Execute<ITradeItemHistoryService, TradeItemHistoryBaseResult>(async (tradeItemHistoryService) =>
            await tradeItemHistoryService.RemoveTradeItemsAsync(request)
        );
    }
}
