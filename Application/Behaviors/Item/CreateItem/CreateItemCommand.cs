using Application.Results.Items;
using MediatR;

namespace Application.Behaviors.Item.CreateItem;

public record CreateItemCommand : IRequest<Result<FullItemResult>>
{
    public required string SenderUserId { get; set; }

    public required string ItemName { get; set; }

    public string ItemDescription { get; set; } = string.Empty;
}
