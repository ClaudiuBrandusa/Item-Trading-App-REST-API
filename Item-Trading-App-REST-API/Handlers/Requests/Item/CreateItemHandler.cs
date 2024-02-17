﻿using Item_Trading_App_REST_API.Models.Item;
using Item_Trading_App_REST_API.Resources.Commands.Item;
using Item_Trading_App_REST_API.Services.Item;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
namespace Item_Trading_App_REST_API.Handlers.Requests.Item;

public class CreateItemHandler : IRequestHandler<CreateItemCommand, FullItemResult>
{
    private readonly IItemService _itemService;

    public CreateItemHandler(IItemService itemService)
    {
        _itemService = itemService;
    }

    public Task<FullItemResult> Handle(CreateItemCommand request, CancellationToken cancellationToken)
    {
        return _itemService.CreateItemAsync(request);
    }
}
