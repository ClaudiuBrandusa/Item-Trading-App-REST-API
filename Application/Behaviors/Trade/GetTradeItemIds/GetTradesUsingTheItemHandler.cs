using Application.Services.Trades;
using MediatR;

namespace Application.Behaviors.Trade.GetTradeItemIds;

public class GetTradesUsingTheItemHandler : IRequestHandler<GetTradesUsingTheItemQuery, string[]>
{
    private readonly ITradeService _tradeService;

    public GetTradesUsingTheItemHandler(ITradeService tradeService)
    {
        _tradeService = tradeService;
    }

    public Task<string[]> Handle(GetTradesUsingTheItemQuery request, CancellationToken cancellationToken)
    {
        return _tradeService.GetItemTradeIdsAsync(request);
    }
}
