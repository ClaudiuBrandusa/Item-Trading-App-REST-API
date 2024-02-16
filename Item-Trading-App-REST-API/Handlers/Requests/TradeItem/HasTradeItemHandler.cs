using Item_Trading_App_REST_API.Resources.Queries.TradeItem;
using Item_Trading_App_REST_API.Services.TradeItem;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Requests.TradeItem;

public class HasTradeItemHandler : IRequestHandler<HasTradeItemQuery, bool>
{
    private readonly ITradeItemService _tradeItemService;

    public HasTradeItemHandler(ITradeItemService tradeItemService)
    {
        _tradeItemService = tradeItemService;
    }

    public Task<bool> Handle(HasTradeItemQuery request, CancellationToken cancellationToken)
    {
        return _tradeItemService.HasTradeItemAsync(request);
    }
}
