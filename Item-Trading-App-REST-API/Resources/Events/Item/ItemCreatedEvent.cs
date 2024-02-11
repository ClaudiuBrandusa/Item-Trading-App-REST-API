using MediatR;

namespace Item_Trading_App_REST_API.Resources.Events.Item;

public record ItemCreatedEvent : INotification
{
    public Entities.Item Item { get; set; }

    public string SenderUserId { get; set; }
}
