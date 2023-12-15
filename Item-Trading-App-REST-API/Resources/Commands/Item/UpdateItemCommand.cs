namespace Item_Trading_App_REST_API.Resources.Commands.Item;

public record UpdateItemCommand : CreateItemCommand
{
    public string ItemId { get; set; }
}
