using Item_Trading_App_REST_API.Models.Item;
using MediatR;

namespace Item_Trading_App_REST_API.Resources.Queries.Inventory;

public record ListInventoryItemsQuery : IRequest<ItemsResult>
{
    public string UserId { get; set; }

    public string SearchString { get; set; }
}
