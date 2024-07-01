using Application.Models.Items;
using MediatR;

namespace Application.Behaviors.Item.ListItems;

public record ListItemsQuery : IRequest<ItemsResult>
{
    public string SearchString { get; set; }
}
