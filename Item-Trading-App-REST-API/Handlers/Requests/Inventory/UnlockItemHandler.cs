using Item_Trading_App_REST_API.Models.Inventory;
using Item_Trading_App_REST_API.Services.Inventory;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Item_Trading_App_REST_API.Resources.Commands.Inventory;

namespace Item_Trading_App_REST_API.Handlers.Requests.Inventory;

public class UnlockItemHandler : IRequestHandler<UnlockItemCommand, LockItemResult>
{
    private readonly IInventoryService _inventoryService;

    public UnlockItemHandler(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public Task<LockItemResult> Handle(UnlockItemCommand request, CancellationToken cancellationToken)
    {
        return _inventoryService.UnlockItemAsync(request);
    }
}
