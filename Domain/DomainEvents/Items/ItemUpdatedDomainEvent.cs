using Domain.Primitives;

namespace Domain.DomainEvents.Items;

public sealed record ItemUpdatedDomainEvent(
    string ItemId,
    string UserId
) : IDomainEvent
{
}
