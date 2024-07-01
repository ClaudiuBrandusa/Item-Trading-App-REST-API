using Application.Models.Items;
using MediatR;

namespace Application.Behaviors.Inventory.ListItems;

public record ListInventoryItemsQuery : IRequest<ItemsResult>
{
    public string UserId { get; set; }

    public string SearchString { get; set; }
}
