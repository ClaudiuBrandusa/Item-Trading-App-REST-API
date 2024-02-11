using MediatR;

namespace Item_Trading_App_REST_API.Resources.Events.Trades;

public record TradeRespondedEvent : INotification
{
    public string TradeId { get; set; }

    public string SenderId { get; set; }

    public bool Response { get; set; }
}
