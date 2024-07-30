using MediatR;
using Application.Services.Item;
using Application.Results.Items;

namespace Application.Behaviors.Item.GetItem;

public class GetItemHandler : IRequestHandler<GetItemQuery, FullItemResult>
{
    private readonly IItemService _itemService;

    public GetItemHandler(IItemService itemService)
    {
        _itemService = itemService;
    }

    public Task<FullItemResult> Handle(GetItemQuery request, CancellationToken cancellationToken)
    {
        return _itemService.GetItemAsync(request);
    }
}
