using Item_Trading_App_REST_API.Requests.Base;
using Item_Trading_App_REST_API.Models.Item;
using Item_Trading_App_REST_API.Services.Inventory;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Requests.Inventory;

public class DropItemHandler : HandlerBase, IRequestHandler<DropItemQuery, QuantifiedItemResult>
{
    public DropItemHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public Task<QuantifiedItemResult> Handle(DropItemQuery request, CancellationToken cancellationToken)
    {
        return Execute<IInventoryService, QuantifiedItemResult>(async (inventoryService) =>
            await inventoryService.DropItemAsync(request.UserId, request.ItemId, request.Quantity, true)
        );
    }
}
