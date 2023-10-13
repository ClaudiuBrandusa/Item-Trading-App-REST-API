namespace Item_Trading_App_REST_API.Models.Inventory;

public record GetUsersItem
{
    public string UserId { get; set; }
    
    public string ItemId { get; set; }
}
