using Item_Trading_App_REST_API.Requests.Base;
using Item_Trading_App_REST_API.Models.Item;
using Item_Trading_App_REST_API.Services.Inventory;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Item_Trading_App_REST_API.Resources.Commands.Inventory;

namespace Item_Trading_App_REST_API.Requests.Inventory;

public class DropInventoryItemHandler : HandlerBase, IRequestHandler<DropInventoryItemCommand, QuantifiedItemResult>
{
    public DropInventoryItemHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public Task<QuantifiedItemResult> Handle(DropInventoryItemCommand request, CancellationToken cancellationToken)
    {
        return Execute<IInventoryService, QuantifiedItemResult>(async (inventoryService) =>
            await inventoryService.DropItemAsync(request)
        );
    }
}
