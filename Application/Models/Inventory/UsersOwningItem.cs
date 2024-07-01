namespace Application.Models.Inventory;

public record UsersOwningItem
{
    public string ItemId { get; set; }

    public string[] UserIds { get; set; }
}
