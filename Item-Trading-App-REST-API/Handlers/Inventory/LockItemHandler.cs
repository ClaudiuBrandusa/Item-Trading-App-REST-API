using Item_Trading_App_REST_API.Requests.Base;
using Item_Trading_App_REST_API.Models.Inventory;
using Item_Trading_App_REST_API.Services.Inventory;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Requests.Inventory;

public class LockItemHandler : HandlerBase, IRequestHandler<LockItemQuery, LockItemResult>
{
    public LockItemHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public Task<LockItemResult> Handle(LockItemQuery request, CancellationToken cancellationToken)
    {
        return Execute<IInventoryService, LockItemResult>(async (inventoryService) =>
            await inventoryService.LockItemAsync(request.UserId, request.ItemId, request.Quantiy, true)
        );
    }
}
