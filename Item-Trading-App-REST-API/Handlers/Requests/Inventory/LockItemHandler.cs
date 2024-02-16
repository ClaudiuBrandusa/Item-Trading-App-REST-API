using Item_Trading_App_REST_API.Models.Inventory;
using Item_Trading_App_REST_API.Services.Inventory;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Item_Trading_App_REST_API.Resources.Commands.Inventory;

namespace Item_Trading_App_REST_API.Handlers.Requests.Inventory;

public class LockItemHandler : IRequestHandler<LockItemCommand, LockItemResult>
{
    private readonly IInventoryService _inventoryService;

    public LockItemHandler(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public Task<LockItemResult> Handle(LockItemCommand request, CancellationToken cancellationToken)
    {
        return _inventoryService.LockItemAsync(request);
    }
}
