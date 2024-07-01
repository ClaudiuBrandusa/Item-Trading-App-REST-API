using MediatR;

namespace Application.Behaviors.Item.DeleteItem;

public record ItemDeletedEvent : INotification
{
    public required string ItemId { get; set; }

    public required string UserId { get; set; }
}
