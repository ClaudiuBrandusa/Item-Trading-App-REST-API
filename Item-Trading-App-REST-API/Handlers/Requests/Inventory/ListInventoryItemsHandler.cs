using Item_Trading_App_REST_API.Models.Item;
using Item_Trading_App_REST_API.Resources.Queries.Inventory;
using Item_Trading_App_REST_API.Services.Inventory;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Requests.Inventory;

public class ListInventoryItemsHandler : IRequestHandler<ListInventoryItemsQuery, ItemsResult>
{
    private readonly IInventoryService _inventoryService;

    public ListInventoryItemsHandler(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public Task<ItemsResult> Handle(ListInventoryItemsQuery request, CancellationToken cancellationToken)
    {
        return _inventoryService.ListItemsAsync(request);
    }
}
