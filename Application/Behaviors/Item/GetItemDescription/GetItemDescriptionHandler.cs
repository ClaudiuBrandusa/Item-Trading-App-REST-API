using Application.Services.Item;
using MediatR;

namespace Application.Behaviors.Item.GetItemDescription;

public class GetItemDescriptionHandler : IRequestHandler<GetItemDescriptionQuery, string>
{
    private readonly IItemService _itemService;

    public GetItemDescriptionHandler(IItemService itemService)
    {
        _itemService = itemService;
    }

    public Task<string> Handle(GetItemDescriptionQuery request, CancellationToken cancellationToken)
    {
        return _itemService.GetItemDescriptionAsync(request);
    }
}
