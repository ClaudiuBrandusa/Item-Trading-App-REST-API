using Domain.Primitives;

namespace Domain.DomainEvents.Trades;

public sealed record TradeItemRemovedDomainEvent(
    string TradeId,
    string ItemId
) : IDomainEvent;