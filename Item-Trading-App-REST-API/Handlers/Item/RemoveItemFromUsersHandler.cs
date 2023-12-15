using Item_Trading_App_REST_API.Requests.Base;
using Item_Trading_App_REST_API.Resources.Commands.Inventory;
using Item_Trading_App_REST_API.Services.Inventory;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Requests.Item;

public class RemoveItemFromUsersHandler : HandlerBase, IRequestHandler<RemoveItemFromUsersCommand>
{
    public RemoveItemFromUsersHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public Task Handle(RemoveItemFromUsersCommand request, CancellationToken cancellationToken)
    {
        return Execute<IInventoryService>(async inventoryService =>
            await inventoryService.RemoveItemCacheAsync(request)
        );
    }
}
