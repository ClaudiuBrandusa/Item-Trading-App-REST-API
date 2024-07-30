using MediatR;

namespace Application.Behaviors.Inventory.DropItem;

public record InventoryItemDroppedEvent : INotification
{
    public string UserId { get; set; } = string.Empty;

    public string ItemId { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public bool Notify { get; set; }
}
