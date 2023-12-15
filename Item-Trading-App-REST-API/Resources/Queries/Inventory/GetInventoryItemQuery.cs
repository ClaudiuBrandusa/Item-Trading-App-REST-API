using Item_Trading_App_REST_API.Models.Item;
using MediatR;

namespace Item_Trading_App_REST_API.Resources.Queries.Inventory;

public record GetInventoryItemQuery : IRequest<QuantifiedItemResult>
{
    public string UserId { get; set; }

    public string ItemId { get; set; }
}
