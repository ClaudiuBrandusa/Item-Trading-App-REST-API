using MediatR;

namespace Application.Behaviors.Trade.GetTradeItemIds;

public record GetTradesUsingTheItemQuery : IRequest<string[]>
{
    public string ItemId { get; set; } = string.Empty;
}
