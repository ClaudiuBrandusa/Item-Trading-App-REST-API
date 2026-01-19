using MediatR;

namespace Application.Behaviors.Inventories.RemoveItemFromUsers;

public record RemoveItemFromUsersCommand : IRequest
{
    public string ItemId { get; set; } = string.Empty;

    public string[] UserIds { get; set; } = Array.Empty<string>();
}
