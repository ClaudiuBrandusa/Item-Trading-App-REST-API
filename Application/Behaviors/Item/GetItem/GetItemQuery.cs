using Application.Results.Items;
using MediatR;

namespace Application.Behaviors.Item.GetItem;

public record GetItemQuery : IRequest<Result<FullItemResult>>
{
    public string ItemId { get; set; } = string.Empty;
}
