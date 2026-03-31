using Application.Results.Trades;
using Application.Services.Trades;
using MediatR;

namespace Application.Behaviors.Trade.CreateTrade;

public class CreateTradeOfferHandler : IRequestHandler<CreateTradeOfferCommand, Result<TradeOfferResult>>
{
    private readonly ITradeService _tradeService;

    public CreateTradeOfferHandler(ITradeService tradeService)
    {
        _tradeService = tradeService;
    }

    public Task<Result<TradeOfferResult>> Handle(CreateTradeOfferCommand request, CancellationToken cancellationToken)
    {
        return _tradeService.CreateTradeOfferAsync(request);
    }
}
