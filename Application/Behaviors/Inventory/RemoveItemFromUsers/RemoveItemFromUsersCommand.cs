using MediatR;

namespace Application.Behaviors.Inventory.RemoveItemFromUsers;

public record RemoveItemFromUsersCommand : IRequest
{
    public string ItemId { get; set; }

    public string[] UserIds { get; set; }
}
