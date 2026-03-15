using Domain.Primitives;

namespace Domain.DomainEvents.Inventories;

public sealed record InventoryItemUnlockedDomainEvent(
    string UserId,
    string ItemId,
    int Quantity,
    bool Notify
) : IDomainEvent
{
}
