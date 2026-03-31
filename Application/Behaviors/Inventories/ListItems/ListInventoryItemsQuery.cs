using Application.Results.Items;
using MediatR;

namespace Application.Behaviors.Inventories.ListItems;

public record ListInventoryItemsQuery : IRequest<Result<ItemsResult>>
{
    public string UserId { get; set; } = string.Empty;

    public string SearchString { get; set; } = string.Empty;
}
