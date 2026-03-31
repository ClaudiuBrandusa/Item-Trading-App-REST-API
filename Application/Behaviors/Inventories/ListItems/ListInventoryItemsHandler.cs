using Application.Results.Items;
using Application.Services.Inventories;
using MediatR;

namespace Application.Behaviors.Inventories.ListItems;

public class ListInventoryItemsHandler : IRequestHandler<ListInventoryItemsQuery, Result<ItemsResult>>
{
    private readonly IInventoryService _inventoryService;

    public ListInventoryItemsHandler(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public Task<Result<ItemsResult>> Handle(ListInventoryItemsQuery request, CancellationToken cancellationToken)
    {
        return _inventoryService.ListItemsAsync(request);
    }
}
