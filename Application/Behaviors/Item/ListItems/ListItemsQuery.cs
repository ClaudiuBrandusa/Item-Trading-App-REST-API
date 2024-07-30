using Application.Results.Items;
using MediatR;

namespace Application.Behaviors.Item.ListItems;

public record ListItemsQuery : IRequest<ItemsResult>
{
    public string SearchString { get; set; } = string.Empty;
}
