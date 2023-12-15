using MediatR;

namespace Item_Trading_App_REST_API.Resources.Queries.Inventory;

public record HasItemQuantityQuery : IRequest<bool>
{
    public string UserId { get; set; }

    public string ItemId { get; set; }

    public int Quantity { get; set; }

    public bool Notify { get; set; }
}
