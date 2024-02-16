using Item_Trading_App_REST_API.Resources.Queries.Item;
using Item_Trading_App_REST_API.Services.TradeItem;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Requests.Item;

public class GetTradesUsingTheItemHandler : IRequestHandler<GetTradesUsingTheItemQuery, string[]>
{
    private readonly ITradeItemService _tradeItemService;

    public GetTradesUsingTheItemHandler(ITradeItemService tradeItemService)
    {
        _tradeItemService = tradeItemService;
    }

    public Task<string[]> Handle(GetTradesUsingTheItemQuery request, CancellationToken cancellationToken)
    {
        return _tradeItemService.GetItemTradeIdsAsync(request);
    }
}
