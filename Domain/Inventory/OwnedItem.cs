using Domain.Identity;
using Domain.Items;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Inventory;

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
