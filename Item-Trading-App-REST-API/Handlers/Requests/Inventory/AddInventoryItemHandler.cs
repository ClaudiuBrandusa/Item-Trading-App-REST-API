using Item_Trading_App_REST_API.Models.Item;
using Item_Trading_App_REST_API.Services.Inventory;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Item_Trading_App_REST_API.Resources.Commands.Inventory;
using Item_Trading_App_REST_API.Handlers.Requests.Base;

namespace Item_Trading_App_REST_API.Handlers.Requests.Inventory;

public class AddInventoryItemHandler : HandlerBase, IRequestHandler<AddInventoryItemCommand, QuantifiedItemResult>
{
    public AddInventoryItemHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public Task<QuantifiedItemResult> Handle(AddInventoryItemCommand request, CancellationToken cancellationToken)
    {
        return Execute(async (IInventoryService inventoryService) =>
            await inventoryService.AddItemAsync(request)
        );
    }
}
