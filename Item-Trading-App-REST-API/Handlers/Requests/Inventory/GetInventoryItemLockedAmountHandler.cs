using Item_Trading_App_REST_API.Handlers.Requests.Base;
using Item_Trading_App_REST_API.Models.Inventory;
using Item_Trading_App_REST_API.Resources.Queries.Inventory;
using Item_Trading_App_REST_API.Services.Inventory;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Requests.Inventory;

public class GetInventoryItemLockedAmountHandler : HandlerBase, IRequestHandler<GetInventoryItemLockedAmountQuery, LockedItemAmountResult>
{
    public GetInventoryItemLockedAmountHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public Task<LockedItemAmountResult> Handle(GetInventoryItemLockedAmountQuery request, CancellationToken cancellationToken)
    {
        return Execute<IInventoryService, LockedItemAmountResult>(async inventoryService =>
            await inventoryService.GetLockedAmountAsync(request)
        );
    }
}
