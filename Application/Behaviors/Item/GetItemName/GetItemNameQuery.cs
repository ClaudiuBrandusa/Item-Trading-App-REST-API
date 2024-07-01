using MediatR;

namespace Application.Behaviors.Item.GetItemName;

public record GetItemNameQuery : IRequest<string>
{
    public required string ItemId { get; set; }
}
