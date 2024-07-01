using MediatR;

namespace Application.Behaviors.Item.GetItemDescription;

public record GetItemDescriptionQuery : IRequest<string>
{
    public required string ItemId { get; set; }
}
