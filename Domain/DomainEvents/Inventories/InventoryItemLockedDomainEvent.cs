// 
using Domain.Primitives;

namespace Domain.DomainEvents.Inventories;

public sealed record InventoryItemLockedDomainEvent(
    string UserId,
    string ItemId,
    int Quantity,
    bool Notify
) : IDomainEvent
{
}
