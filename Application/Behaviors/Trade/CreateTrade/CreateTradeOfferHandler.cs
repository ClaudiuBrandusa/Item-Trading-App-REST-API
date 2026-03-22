using Application.Results.Trades;
using Application.Services.Trades;
using MediatR;

namespace Application.Behaviors.Trade.CreateTrade;

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
