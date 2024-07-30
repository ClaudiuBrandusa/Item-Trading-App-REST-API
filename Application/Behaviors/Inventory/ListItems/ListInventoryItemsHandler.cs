using Application.Results.Items;
using Application.Services.Inventory;
using MediatR;

namespace Application.Behaviors.Inventory.ListItems;

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
