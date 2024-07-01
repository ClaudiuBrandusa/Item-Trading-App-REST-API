using MediatR;

namespace Application.Behaviors.Inventory.HasItem;

public record HasItemQuantityQuery : IRequest<bool>
{
    public string UserId { get; set; }

    public string ItemId { get; set; }

    public int Quantity { get; set; }

    public bool Notify { get; set; }
}
