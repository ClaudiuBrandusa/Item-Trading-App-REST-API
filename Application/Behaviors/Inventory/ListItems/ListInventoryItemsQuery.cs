using Application.Results.Items;
using MediatR;

namespace Application.Behaviors.Inventory.ListItems;

public record ListInventoryItemsQuery : IRequest<ItemsResult>
{
    public string UserId { get; set; } = string.Empty;

    public string SearchString { get; set; } = string.Empty;
}
