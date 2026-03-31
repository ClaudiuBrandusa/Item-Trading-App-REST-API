using Application.Results.Trades;
using Application.Services.Trades;
using MediatR;

namespace Application.Behaviors.Trade.RespondTrade;

public class RespondTradeHandler : IRequestHandler<RespondTradeCommand, Result<TradeOfferResult>>
{
    private readonly ITradeService _tradeService;

    public RespondTradeHandler(ITradeService tradeService)
    {
        _tradeService = tradeService;
    }

    public Task<Result<TradeOfferResult>> Handle(RespondTradeCommand request, CancellationToken cancellationToken)
    {
        if (request.Response)
            return _tradeService.AcceptTradeOfferAsync(request);
        else
            return _tradeService.RejectTradeOfferAsync(request);
    }
}
