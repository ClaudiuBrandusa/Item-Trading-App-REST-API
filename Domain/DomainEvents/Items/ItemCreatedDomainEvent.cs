using Domain.Entities.Items;
using Domain.Primitives;

namespace Domain.DomainEvents.Items;

public sealed record ItemCreatedDomainEvent(
    Item Item,
    string SenderUserId
) : IDomainEvent
{
}
