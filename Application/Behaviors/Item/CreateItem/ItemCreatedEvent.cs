using MediatR;

namespace Application.Behaviors.Item.CreateItem;

public record ItemCreatedEvent : INotification
{
    public required Domain.Items.Item Item { get; set; }

    public required string SenderUserId { get; set; }
}
