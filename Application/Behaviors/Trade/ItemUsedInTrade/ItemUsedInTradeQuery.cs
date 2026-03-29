using MediatR;

namespace Application.Behaviors.Trade.ItemUsedInTrade;

public record ItemUsedInTradeQuery : IRequest<bool>
{
    public string ItemId { get; set; } = string.Empty;
}
