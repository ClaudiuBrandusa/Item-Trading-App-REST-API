using Domain.Primitives;

namespace Domain.DomainEvents.Trades;

public sealed record TradeRespondedDomainEvent(
    string TradeId,
    string SenderId,
    bool Response
) : IDomainEvent;