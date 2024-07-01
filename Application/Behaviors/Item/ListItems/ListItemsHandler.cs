using Application.Models.Items;
using Application.Services.Item;
using MediatR;

namespace Application.Behaviors.Item.ListItems;

public class ListItemsHandler : IRequestHandler<ListItemsQuery, ItemsResult>
{
    private readonly IItemService _itemService;

    public ListItemsHandler(IItemService itemService)
    {
        _itemService = itemService;
    }

    public Task<ItemsResult> Handle(ListItemsQuery request, CancellationToken cancellationToken)
    {
        return _itemService.ListItemsAsync(request);
    }
}
