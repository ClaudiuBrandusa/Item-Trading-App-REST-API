using MediatR;

namespace Application.Behaviors.Trade.CancelTrade;

public class TradeCancelledEvent : INotification
{
    public string TradeId { get; set; } = string.Empty;

    public string ReceiverId { get; set; } = string.Empty;
}
