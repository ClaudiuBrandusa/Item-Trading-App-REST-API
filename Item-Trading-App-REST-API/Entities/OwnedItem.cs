using System.ComponentModel.DataAnnotations.Schema;

namespace Item_Trading_App_REST_API.Entities;

public class OwnedItem
{
    public string UserId { get; set; }

    public string ItemId { get; set; }

    public int Quantity { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; }

    [ForeignKey(nameof(ItemId))]
    public Item Item { get; set; }

    public LockedItem LockedItem { get; set; }
}
