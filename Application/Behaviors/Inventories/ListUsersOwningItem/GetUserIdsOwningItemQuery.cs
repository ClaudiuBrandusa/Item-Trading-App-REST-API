using Application.Models.Inventories;
using MediatR;

namespace Application.Behaviors.Inventories.ListUsersOwningItem;

public record GetUserIdsOwningItemQuery : IRequest<Result<UsersOwningItem>>
{
    public required string ItemId { get; set; }
}
