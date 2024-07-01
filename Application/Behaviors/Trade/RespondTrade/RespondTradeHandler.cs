using Application.Models.Trade;
using Application.Services.Trade;
using MediatR;

namespace Application.Behaviors.Trade.RespondTrade;

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
