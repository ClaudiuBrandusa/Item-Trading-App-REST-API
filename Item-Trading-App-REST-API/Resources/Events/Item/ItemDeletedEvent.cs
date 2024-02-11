using MediatR;

namespace Item_Trading_App_REST_API.Resources.Events.Item;

public record ItemDeletedEvent : INotification
{
    public string ItemId { get; set; }

    public string UserId { get; set; }
}
