using Application.Models.Items;
using MediatR;

namespace Application.Behaviors.Item.GetItem;

public record GetItemQuery : IRequest<FullItemResult>
{
    public string ItemId { get; set; }
}
