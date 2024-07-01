using Application.Models.Items;
using Application.Services.Item;
using MediatR;

namespace Application.Behaviors.Item.DeleteItem;

public class DeleteItemHandler : IRequestHandler<DeleteItemCommand, DeleteItemResult>
{
    private readonly IItemService _itemService;

    public DeleteItemHandler(IItemService itemService)
    {
        _itemService = itemService;
    }

    public Task<DeleteItemResult> Handle(DeleteItemCommand request, CancellationToken cancellationToken)
    {
        return _itemService.DeleteItemAsync(request);
    }
}
