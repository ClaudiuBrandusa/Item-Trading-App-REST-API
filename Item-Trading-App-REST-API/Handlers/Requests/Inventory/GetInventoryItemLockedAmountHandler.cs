using Item_Trading_App_REST_API.Models.Inventory;
using Item_Trading_App_REST_API.Resources.Queries.Inventory;
using Item_Trading_App_REST_API.Services.Inventory;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Requests.Inventory;

public class GetInventoryItemLockedAmountHandler : IRequestHandler<GetInventoryItemLockedAmountQuery, LockedItemAmountResult>
{
    private readonly IInventoryService _inventoryService;

    public GetInventoryItemLockedAmountHandler(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public Task<LockedItemAmountResult> Handle(GetInventoryItemLockedAmountQuery request, CancellationToken cancellationToken)
    {
        return _inventoryService.GetLockedAmountAsync(request);
    }
}
