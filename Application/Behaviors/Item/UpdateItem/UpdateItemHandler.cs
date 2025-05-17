using Application.Results.Items;
using Application.Services.Item;
using MediatR;

namespace Application.Behaviors.Item.UpdateItem;

public class UpdateItemHandler : IRequestHandler<UpdateItemCommand, FullItemResult>
{
    private readonly IItemService _itemService;

    public UpdateItemHandler(IItemService itemService)
    {
        _itemService = itemService;
    }

    public Task<FullItemResult> Handle(UpdateItemCommand request, CancellationToken cancellationToken)
    {
        return _itemService.UpdateItemAsync(request);
    }
}
