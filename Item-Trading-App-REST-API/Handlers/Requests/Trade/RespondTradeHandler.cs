using Item_Trading_App_REST_API.Models.Trade;
using Item_Trading_App_REST_API.Resources.Commands.Trade;
using Item_Trading_App_REST_API.Services.Trade;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Requests.Trade;

public class RespondTradeHandler : IRequestHandler<RespondTradeCommand, TradeOfferResult>
{
    private readonly ITradeService _tradeService;

    public RespondTradeHandler(ITradeService tradeService)
    {
        _tradeService = tradeService;
    }

    public Task<TradeOfferResult> Handle(RespondTradeCommand request, CancellationToken cancellationToken)
    {
        if (request.Response)
            return _tradeService.AcceptTradeOfferAsync(request);
        else
            return _tradeService.RejectTradeOfferAsync(request);
    }
}
