using Application.Behaviors.Item.CreateItem;

namespace Application.Behaviors.Item.UpdateItem;

public record UpdateItemCommand : CreateItemCommand
{
    public string ItemId { get; set; }
}
