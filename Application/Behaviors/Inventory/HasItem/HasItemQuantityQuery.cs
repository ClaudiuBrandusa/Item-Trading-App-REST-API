using MediatR;

namespace Application.Behaviors.Inventory.HasItem;

public record HasItemQuantityQuery : IRequest<bool>
{
    public string UserId { get; set; } = string.Empty;

    public string ItemId { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public bool Notify { get; set; }
}
