﻿using Item_Trading_App_REST_API.Models.Item;
using Item_Trading_App_REST_API.Services.Inventory;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Item_Trading_App_REST_API.Resources.Commands.Inventory;

namespace Item_Trading_App_REST_API.Handlers.Requests.Inventory;

public class AddInventoryItemHandler : IRequestHandler<AddInventoryItemCommand, QuantifiedItemResult>
{
    private readonly IInventoryService _inventoryService;

    public AddInventoryItemHandler(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public Task<QuantifiedItemResult> Handle(AddInventoryItemCommand request, CancellationToken cancellationToken)
    {
        return _inventoryService.AddItemAsync(request);
    }
}
