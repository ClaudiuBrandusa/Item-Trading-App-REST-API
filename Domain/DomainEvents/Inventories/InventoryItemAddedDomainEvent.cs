using Domain.Primitives;

namespace Domain.DomainEvents.Inventories;

public sealed record InventoryItemAddedDomainEvent(
    string UserId,
    string ItemId,
    int Quantity,
    bool Notify
) : IDomainEvent
{
}
