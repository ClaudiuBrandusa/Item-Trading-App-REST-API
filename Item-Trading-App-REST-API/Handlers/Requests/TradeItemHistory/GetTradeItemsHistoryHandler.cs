using Item_Trading_App_REST_API.Resources.Queries.TradeItemHistory;
using Item_Trading_App_REST_API.Services.TradeItemHistory;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Requests.TradeItemHistory;

public class GetTradeItemsHistoryHandler : IRequestHandler<GetTradeItemsHistoryQuery, Models.TradeItems.TradeItem[]>
{
    private readonly ITradeItemHistoryService _tradeItemHistoryService;

    public GetTradeItemsHistoryHandler(ITradeItemHistoryService tradeItemHistoryService)
    {
        _tradeItemHistoryService = tradeItemHistoryService;
    }

    public Task<Models.TradeItems.TradeItem[]> Handle(GetTradeItemsHistoryQuery request, CancellationToken cancellationToken)
    {
        return _tradeItemHistoryService.GetTradeItemsAsync(request);
    }
}
