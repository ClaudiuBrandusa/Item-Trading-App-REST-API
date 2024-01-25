using Item_Trading_App_REST_API.Models.TradeItemHistory;
using Item_Trading_App_REST_API.Requests.Base;
using Item_Trading_App_REST_API.Resources.Commands.TradeItemHistory;
using Item_Trading_App_REST_API.Services.TradeItemHistory;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.TradeItemHistory;

public class AddTradeItemsHistoryHandler : HandlerBase, IRequestHandler<AddTradeItemsHistoryCommand, TradeItemHistoryBaseResult>
{
    public AddTradeItemsHistoryHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public Task<TradeItemHistoryBaseResult> Handle(AddTradeItemsHistoryCommand request, CancellationToken cancellationToken)
    {
        return Execute(async (ITradeItemHistoryService tradeItemHistoryService) =>
            await tradeItemHistoryService.AddTradeItemsAsync(request)
        );
    }
}
