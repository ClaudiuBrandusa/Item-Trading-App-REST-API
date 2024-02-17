using Item_Trading_App_REST_API.Resources.Queries.TradeItem;
using Item_Trading_App_REST_API.Services.TradeItem;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Requests.TradeItem;

public class GetTradeItemsHandler : IRequestHandler<GetTradeItemsQuery, Models.TradeItems.TradeItem[]>
{
    private readonly ITradeItemService _tradeItemService;

    public GetTradeItemsHandler(ITradeItemService tradeItemService)
    {
        _tradeItemService = tradeItemService;
    }

    public Task<Models.TradeItems.TradeItem[]> Handle(GetTradeItemsQuery request, CancellationToken cancellationToken)
    {
        return _tradeItemService.GetTradeItemsAsync(request);
    }
}
