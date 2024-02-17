using Item_Trading_App_REST_API.Models.Item;
using Item_Trading_App_REST_API.Resources.Queries.Inventory;
using Item_Trading_App_REST_API.Services.Inventory;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Requests.Inventory;

public class GetInventoryItemHandler : IRequestHandler<GetInventoryItemQuery, QuantifiedItemResult>
{
    private readonly IInventoryService _inventoryService;

    public GetInventoryItemHandler(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public Task<QuantifiedItemResult> Handle(GetInventoryItemQuery request, CancellationToken cancellationToken)
    {
        return _inventoryService.GetItemAsync(request);
    }
}
