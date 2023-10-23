namespace Item_Trading_App_REST_API.Models.Item;

public record UpdateItem : CreateItem
{
    public string ItemId { get; set; }
}
