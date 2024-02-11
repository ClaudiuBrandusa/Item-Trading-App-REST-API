using Item_Trading_App_REST_API.Handlers.Requests.Base;
using Item_Trading_App_REST_API.Resources.Commands.TradeItem;
using Item_Trading_App_REST_API.Services.TradeItem;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Requests.TradeItem;

public class AddTradeItemHandler : HandlerBase, IRequestHandler<AddTradeItemCommand, bool>
{
    public AddTradeItemHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public Task<bool> Handle(AddTradeItemCommand request, CancellationToken cancellationToken)
    {
        return Execute<ITradeItemService, bool>(async tradeItemService =>
            await tradeItemService.AddTradeItemAsync(request)
        );
    }
}
