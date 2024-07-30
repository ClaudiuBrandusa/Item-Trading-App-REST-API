using System.ComponentModel.DataAnnotations;

namespace Domain.Entities.Items;

public class Item : Entity
{
    [Key]
    public string ItemId { get; private set; }

    [Required]
    public string Name { get; private set; }

    public string Description { get; private set; } = string.Empty;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private Item() {}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public Item(string itemId, string name, string description = "")
    {
        ItemId = itemId;
        Name = name;
        Description = description;
    }

    public Item(string name, string description = "") : this(GenerateId(), name, description)
    {
    }

    public void UpdateItemName(string itemName) => Name = itemName;

    public void UpdateItemDescription(string itemDescription) => Description = itemDescription;

    public static string GenerateId() => Guid.NewGuid().ToString();

    protected override bool Compare(object obj)
    {
        if (obj is not Item entity) return false;

        return entity.ItemId == ItemId &&
               entity.Name == Name &&
               entity.Description == Description;
    }

    protected override object GetId() => ItemId;
}
