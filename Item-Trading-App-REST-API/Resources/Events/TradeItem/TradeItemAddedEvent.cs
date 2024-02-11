using MediatR;

namespace Item_Trading_App_REST_API.Resources.Events.TradeItem;

public record TradeItemAddedEvent : INotification
{
    public string TradeId { get; set; }

    public Models.TradeItems.TradeItem Data { get; set; }
}
