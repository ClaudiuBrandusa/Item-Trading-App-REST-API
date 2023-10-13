using System.Collections.Generic;

namespace Item_Trading_App_REST_API.Models.Inventory;

public record RemoveItemFromUsers
{
    public string ItemId { get; set; }

    public List<string> UserIds { get; set; }
}
