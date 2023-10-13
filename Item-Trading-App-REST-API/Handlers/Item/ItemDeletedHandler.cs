﻿using Item_Trading_App_REST_API.Models.Inventory;
using Item_Trading_App_REST_API.Requests.Base;
using Item_Trading_App_REST_API.Services.Inventory;
using MapsterMapper;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Requests.Item;

public class ItemDeletedHandler : HandlerBase, IRequestHandler<ItemDeleted>
{
    public ItemDeletedHandler(IServiceProvider serviceProvider, IMapper mapper) : base(serviceProvider, mapper)
    {
    }

    public async Task Handle(ItemDeleted request, CancellationToken cancellationToken)
    {
        await Execute<IInventoryService>(async inventoryService =>
            await inventoryService.RemoveItemAsync(Map<ItemDeleted, RemoveItemFromUsers>(request))
        );
    }
}
