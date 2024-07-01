using Application.Models.Items;
using MediatR;

namespace Application.Behaviors.Item.DeleteItem;

public record DeleteItemCommand : IRequest<DeleteItemResult>
{
    public required string ItemId { get; set; }

    public required string UserId { get; set; }
}
