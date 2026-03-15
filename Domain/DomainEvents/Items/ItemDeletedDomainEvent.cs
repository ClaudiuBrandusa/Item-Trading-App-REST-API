using Domain.Primitives;

namespace Domain.DomainEvents.Items;

public sealed record ItemDeletedDomainEvent(
    string ItemId,
    string UserId
) : IDomainEvent
{
}
