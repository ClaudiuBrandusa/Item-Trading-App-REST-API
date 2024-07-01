using Application.Services.Item;
using MediatR;

namespace Application.Behaviors.Item.GetItemName;

public class GetItemNameHandler : IRequestHandler<GetItemNameQuery, string>
{
    private readonly IItemService _itemService;

    public GetItemNameHandler(IItemService itemService)
    {
        _itemService = itemService;
    }

    public Task<string> Handle(GetItemNameQuery request, CancellationToken cancellationToken)
    {
        return _itemService.GetItemNameAsync(request);
    }
}
