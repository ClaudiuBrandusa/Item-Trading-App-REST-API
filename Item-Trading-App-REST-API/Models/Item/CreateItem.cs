namespace Item_Trading_App_REST_API.Models.Item;

public record CreateItem
{
    public string SenderUserId { get; set; }

    public string ItemName { get; set; }

    public string ItemDescription { get; set; }
}
