using Item_Trading_App_REST_API.Requests.Base;
using Item_Trading_App_REST_API.Requests.TradeItem;
using Item_Trading_App_REST_API.Services.TradeItem;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.TradeContent;

public class AddTradeContentHandler : HandlerBase, IRequestHandler<AddTradeItemRequest, bool>
{
    public AddTradeContentHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public Task<bool> Handle(AddTradeItemRequest request, CancellationToken cancellationToken)
    {
        return Execute<ITradeItemService, bool>(async tradeItemService =>
            await tradeItemService.AddTradeItemAsync(request)
        );
    }
}
