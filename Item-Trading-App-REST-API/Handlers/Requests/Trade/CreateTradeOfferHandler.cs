using Item_Trading_App_REST_API.Models.Trade;
using Item_Trading_App_REST_API.Resources.Commands.Trade;
using Item_Trading_App_REST_API.Services.Trade;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Requests.Trade;

public class CreateTradeOfferHandler : IRequestHandler<CreateTradeOfferCommand, TradeOfferResult>
{
    private readonly ITradeService _tradeService;

    public CreateTradeOfferHandler(ITradeService tradeService)
    {
        _tradeService = tradeService;
    }

    public Task<TradeOfferResult> Handle(CreateTradeOfferCommand request, CancellationToken cancellationToken)
    {
        return _tradeService.CreateTradeOfferAsync(request);
    }
}
