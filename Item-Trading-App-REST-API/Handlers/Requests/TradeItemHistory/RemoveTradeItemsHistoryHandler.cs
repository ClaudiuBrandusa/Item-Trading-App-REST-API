using Item_Trading_App_REST_API.Models.TradeItemHistory;
using Item_Trading_App_REST_API.Resources.Commands.TradeItemHistory;
using Item_Trading_App_REST_API.Services.TradeItemHistory;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Requests.TradeItemHistory;

public class RemoveTradeItemsHistoryHandler : IRequestHandler<RemoveTradeItemsHistoryCommand, TradeItemHistoryBaseResult>
{
    private readonly ITradeItemHistoryService _tradeItemHistoryService;

    public RemoveTradeItemsHistoryHandler(ITradeItemHistoryService tradeItemHistoryService)
    {
        _tradeItemHistoryService = tradeItemHistoryService;
    }

    public Task<TradeItemHistoryBaseResult> Handle(RemoveTradeItemsHistoryCommand request, CancellationToken cancellationToken)
    {
        return _tradeItemHistoryService.RemoveTradeItemsAsync(request);
    }
}
