using Application.Services.TradeItem;
using MediatR;

namespace Application.Behaviors.TradeItem.AddTradeItem;

public class AddTradeItemHandler : IRequestHandler<AddTradeItemCommand, bool>
{
    private readonly ITradeItemService _tradeItemService;

    public AddTradeItemHandler(ITradeItemService tradeItemService)
    {
        _tradeItemService = tradeItemService;
    }

    public Task<bool> Handle(AddTradeItemCommand request, CancellationToken cancellationToken)
    {
        return _tradeItemService.AddTradeItemAsync(request);
    }
}
