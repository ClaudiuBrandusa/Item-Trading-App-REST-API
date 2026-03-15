using Domain.Entities.Trades;
using Domain.Primitives;

namespace Domain.DomainEvents.Trades;

public sealed record TradeItemAddedDomainEvent(
    string TradeId,
    TradeItem Data
) : IDomainEvent;