using MediatR;

namespace Item_Trading_App_REST_API.Resources.Events.TradeItem;

public record TradeItemRemovedEvent : INotification
{
    public string TradeId { get; set; }

    public bool KeepCache { get; set; }
}
