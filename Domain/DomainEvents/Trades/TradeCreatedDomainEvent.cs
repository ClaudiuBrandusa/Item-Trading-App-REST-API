using Domain.Primitives;

namespace Domain.DomainEvents.Trades;

public sealed record TradeCreatedDomainEvent(
    string TradeId,
    string ReceiverId
) : IDomainEvent;