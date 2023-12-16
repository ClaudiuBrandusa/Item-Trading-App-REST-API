using MediatR;

namespace Item_Trading_App_REST_API.Resources.Commands.Inventory;

public record RemoveItemFromUsersCommand : IRequest
{
    public string ItemId { get; set; }

    public string[] UserIds { get; set; }
}
