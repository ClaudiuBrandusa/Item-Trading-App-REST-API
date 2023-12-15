using Item_Trading_App_REST_API.Models.Inventory;
using MediatR;

namespace Item_Trading_App_REST_API.Resources.Queries.Inventory;

public record GetUserIdsOwningItemQuery : IRequest<UsersOwningItem>
{
    public string ItemId { get; set; }
}
