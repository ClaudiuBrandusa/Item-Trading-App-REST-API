using Item_Trading_App_REST_API.Requests.Base;
using Item_Trading_App_REST_API.Models.Item;
using Item_Trading_App_REST_API.Services.Inventory;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Requests.Inventory;

public class AddItemHandler : HandlerBase, IRequestHandler<AddItemQuery, QuantifiedItemResult>
{
    public AddItemHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public async Task<QuantifiedItemResult> Handle(AddItemQuery request, CancellationToken cancellationToken)
    {
        return await Execute<IInventoryService, QuantifiedItemResult>(async (inventoryService) =>
            await inventoryService.AddItemAsync(request.UserId, request.ItemId, request.Quantity, true)
        );
    }
}
