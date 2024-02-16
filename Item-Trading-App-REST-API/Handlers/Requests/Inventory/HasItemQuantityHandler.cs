using Item_Trading_App_REST_API.Resources.Queries.Inventory;
using Item_Trading_App_REST_API.Services.Inventory;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Requests.Inventory;

public class HasItemQuantityHandler : IRequestHandler<HasItemQuantityQuery, bool>
{
    private readonly IInventoryService _inventoryService;

    public HasItemQuantityHandler(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public Task<bool> Handle(HasItemQuantityQuery request, CancellationToken cancellationToken)
    {
        return _inventoryService.HasItemAsync(request);
    }
}
