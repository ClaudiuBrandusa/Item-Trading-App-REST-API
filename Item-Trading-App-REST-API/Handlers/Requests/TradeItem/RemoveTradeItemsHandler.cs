using Item_Trading_App_REST_API.Resources.Commands.TradeItem;
using Item_Trading_App_REST_API.Services.TradeItem;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Requests.TradeItem;

public class RemoveTradeItemsHandler : IRequestHandler<RemoveTradeItemsCommand, bool>
{
    private readonly ITradeItemService _tradeItemService;

    public RemoveTradeItemsHandler(ITradeItemService tradeItemService)
    {
        _tradeItemService = tradeItemService;
    }

    public Task<bool> Handle(RemoveTradeItemsCommand request, CancellationToken cancellationToken)
    {
        return _tradeItemService.RemoveTradeItemsAsync(request);
    }
}
