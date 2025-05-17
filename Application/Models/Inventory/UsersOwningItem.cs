namespace Application.Models.Inventory;

public record UsersOwningItem
{
    public string ItemId { get; set; } = string.Empty;

    public string[] UserIds { get; set; } = Array.Empty<string>();
}
