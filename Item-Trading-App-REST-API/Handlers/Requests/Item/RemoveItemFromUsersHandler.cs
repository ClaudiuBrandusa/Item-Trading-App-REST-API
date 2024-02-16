using Item_Trading_App_REST_API.Resources.Commands.Inventory;
using Item_Trading_App_REST_API.Services.Inventory;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Requests.Item;

public class RemoveItemFromUsersHandler : IRequestHandler<RemoveItemFromUsersCommand>
{
    private readonly IInventoryService _inventoryService;

    public RemoveItemFromUsersHandler(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public Task Handle(RemoveItemFromUsersCommand request, CancellationToken cancellationToken)
    {
        return _inventoryService.RemoveItemCacheAsync(request);
    }
}
