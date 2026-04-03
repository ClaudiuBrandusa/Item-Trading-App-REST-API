using Domain.Primitives;

namespace Domain.DomainEvents.Items;

public sealed record ItemCreatedDomainEvent(
    string ItemId,
    string UserId
) : IDomainEvent
{
}
