using Domain.Primitives;

namespace Domain.DomainEvents.Trades;

public sealed record TradeCancelledDomainEvent(
    string TradeId,
    string ReceiverId
) : IDomainEvent;