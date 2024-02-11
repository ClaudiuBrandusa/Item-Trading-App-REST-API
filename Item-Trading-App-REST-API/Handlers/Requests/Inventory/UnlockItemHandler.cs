using Item_Trading_App_REST_API.Models.Inventory;
using Item_Trading_App_REST_API.Services.Inventory;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Item_Trading_App_REST_API.Resources.Commands.Inventory;
using Item_Trading_App_REST_API.Handlers.Requests.Base;

namespace Item_Trading_App_REST_API.Handlers.Requests.Inventory;

public class UnlockItemHandler : HandlerBase, IRequestHandler<UnlockItemCommand, LockItemResult>
{
    public UnlockItemHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public Task<LockItemResult> Handle(UnlockItemCommand request, CancellationToken cancellationToken)
    {
        return Execute<IInventoryService, LockItemResult>(async (inventoryService) =>
            await inventoryService.UnlockItemAsync(request)
        );
    }
}
