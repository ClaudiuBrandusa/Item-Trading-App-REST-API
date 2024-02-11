using MediatR;

namespace Item_Trading_App_REST_API.Resources.Events.Trades;

public record TradeCreatedEvent : INotification
{
    public string TradeId { get; set; }

    public string ReceiverId {  get; set; }
}
