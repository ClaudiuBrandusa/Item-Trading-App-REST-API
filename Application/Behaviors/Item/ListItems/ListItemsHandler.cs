using Application.Results.Items;
using Application.Services.Item;
using MediatR;

namespace Application.Behaviors.Item.ListItems;

public class ListItemsHandler : IRequestHandler<ListItemsQuery, Result<ItemsResult>>
{
    private readonly IItemService _itemService;

    public ListItemsHandler(IItemService itemService)
    {
        _itemService = itemService;
    }

    public Task<Result<ItemsResult>> Handle(ListItemsQuery request, CancellationToken cancellationToken)
    {
        return _itemService.ListItemsAsync(request);
    }
}
