using Domain.Primitives;

namespace Domain.DomainEvents.Trades;

public sealed record TradeItemAddedDomainEvent(
    string TradeId,
    string ItemId,
    int Quantity,
    int Price
) : IDomainEvent;