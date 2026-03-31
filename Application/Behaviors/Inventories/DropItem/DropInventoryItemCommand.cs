using Application.Results.Inventories;
using MediatR;

namespace Application.Behaviors.Inventories.DropItem;

public record DropInventoryItemCommand : IRequest<Result<QuantifiedItemResult>>
{
    public required string UserId { get; set; }

    public required string ItemId { get; set; }

    public int Quantity { get; set; }

    public bool Notify { get; set; }
}
