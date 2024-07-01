using Application.Models.Inventory;
using MediatR;

namespace Application.Behaviors.Inventory.ListUsersOwningItem;

public record GetUserIdsOwningItemQuery : IRequest<UsersOwningItem>
{
    public required string ItemId { get; set; }
}
