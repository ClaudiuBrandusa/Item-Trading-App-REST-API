﻿using Item_Trading_App_REST_API.Requests.Base;
using Item_Trading_App_REST_API.Models.Inventory;
using Item_Trading_App_REST_API.Services.Inventory;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using MapsterMapper;

namespace Item_Trading_App_REST_API.Requests.Inventory;

public class UnlockItemHandler : HandlerBase, IRequestHandler<UnlockItemQuery, LockItemResult>
{
    public UnlockItemHandler(IServiceProvider serviceProvider, IMapper mapper) : base(serviceProvider, mapper)
    {
    }

    public Task<LockItemResult> Handle(UnlockItemQuery request, CancellationToken cancellationToken)
    {
        return Execute<IInventoryService, LockItemResult>(async (inventoryService) =>
            await inventoryService.UnlockItemAsync(Map<UnlockItemQuery, LockInventoryItem>(request), true)
        );
    }
}