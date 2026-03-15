using Domain.Entities.Items;
using Domain.Primitives;

namespace Domain.DomainEvents.Items;

public sealed record ItemUpdatedDomainEvent(
    Item Item,
    string SenderUserId
) : IDomainEvent
{
}
