using MediatR;

namespace Application.Behaviors.Trade.GetTradeItemIds;

public record GetTradesUsingTheItemQuery : IRequest<Result<string[]>>
{
    public string ItemId { get; set; } = string.Empty;
}
