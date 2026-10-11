using MediatR;

namespace Application.Behaviors.Item.GetItemName;

public record GetItemsNameQuery : IRequest<Dictionary<string, string>>
{
    public required string[] ItemIds { get; set; } = [];
}
