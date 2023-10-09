using Item_Trading_App_REST_API.Requests.Base;
using Item_Trading_App_REST_API.Models.Inventory;
using Item_Trading_App_REST_API.Services.Inventory;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Requests.Inventory;

public class UnlockItemHandler : HandlerBase, IRequestHandler<UnlockItemQuery, LockItemResult>
{
    public UnlockItemHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public async Task<LockItemResult> Handle(UnlockItemQuery request, CancellationToken cancellationToken)
    {
        return await Execute<IInventoryService, LockItemResult>(async (inventoryService) =>
            await inventoryService.UnlockItemAsync(request.UserId, request.ItemId, request.Quantity, true)
        );
    }
}
