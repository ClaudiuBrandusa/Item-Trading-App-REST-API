using Application.Results.Inventory;
using MediatR;

namespace Application.Behaviors.Inventory.DropItem;

public record DropInventoryItemCommand : IRequest<QuantifiedItemResult>
{
    public required string UserId { get; set; }

    public required string ItemId { get; set; }

    public int Quantity { get; set; }

    public bool Notify { get; set; }
}
