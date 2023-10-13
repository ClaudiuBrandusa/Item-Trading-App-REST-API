namespace Item_Trading_App_REST_API.Models.Inventory;

public record ListItems
{
    public string UserId { get; set; }

    public string SearchString { get; set; }
}
