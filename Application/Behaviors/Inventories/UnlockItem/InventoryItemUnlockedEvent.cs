using MediatR;

namespace Application.Behaviors.Inventories.UnlockItem;

public record InventoryItemUnlockedEvent : INotification
{
    public required string UserId { get; set; }

    public required string ItemId { get; set; }

    public int Quantity { get; set; }

    public bool Notify { get; set; }
}
