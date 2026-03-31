using Application.Services.Trades;
using MediatR;

namespace Application.Behaviors.Trade.GetTradeItemIds;

public class GetTradesUsingTheItemHandler : IRequestHandler<GetTradesUsingTheItemQuery, Result<string[]>>
{
    private readonly ITradeService _tradeService;

    public GetTradesUsingTheItemHandler(ITradeService tradeService)
    {
        _tradeService = tradeService;
    }

    public Task<Result<string[]>> Handle(GetTradesUsingTheItemQuery request, CancellationToken cancellationToken)
    {
        return _tradeService.GetItemTradeIdsAsync(request);
    }
}
