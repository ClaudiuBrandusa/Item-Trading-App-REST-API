using Application.Results.Items;
using MediatR;

namespace Application.Behaviors.Item.GetItem;

public record GetItemsQuery : IRequest<Result<FullItemResult[]>>
{
    public string[] ItemIds { get; set; } = [];
}
