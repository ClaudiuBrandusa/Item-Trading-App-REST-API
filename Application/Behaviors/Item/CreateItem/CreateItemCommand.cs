using Application.Models.Items;
using MediatR;

namespace Application.Behaviors.Item.CreateItem;

public record CreateItemCommand : IRequest<FullItemResult>
{
    public string SenderUserId { get; set; }

    public string ItemName { get; set; }

    public string ItemDescription { get; set; }
}
