using MediatR;

namespace Application.Behaviors.Item.UpdateItem;

public record ItemUpdatedEvent : INotification
{
    public Domain.Items.Item Item { get; set; }

    public string SenderUserId { get; set; }
}
