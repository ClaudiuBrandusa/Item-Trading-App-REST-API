namespace Domain.Entities.Items;

public class Item : Entity
{
    public const int MinimumNameLength = 3;

    public string ItemId { get; private set; }

    public string Name { get; private set; }

    public string Description { get; private set; } = string.Empty;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private Item() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public Item(string itemId, string name, string description = "")
    {
        ArgumentException.ThrowIfNullOrEmpty(itemId, nameof(ItemId));
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(Name));

        if (!ValidateName(name))
            ThrowNameLengthException();

        ItemId = itemId;
        Name = name;
        Description = description;
    }

    public Item(string name, string description = "") : this(GenerateId(), name, description)
    {
    }

    public void UpdateItemName(string itemName)
    {
        ArgumentException.ThrowIfNullOrEmpty(itemName, nameof(Name));

        if (!ValidateName(itemName))
            ThrowNameLengthException();

        Name = itemName;
    }

    public void UpdateItemDescription(string itemDescription) => Description = itemDescription;

    public static string GenerateId() => Guid.NewGuid().ToString();

    private bool ValidateName(string name)
    {
        return name.Length >= MinimumNameLength;
    }

    private void ThrowNameLengthException() => throw new ArgumentException($"{nameof(Name)} length needs to be at least 3 characters.");

    protected override bool Compare(object obj)
    {
        if (obj is not Item entity) return false;

        return entity.ItemId == ItemId &&
               entity.Name == Name &&
               entity.Description == Description;
    }

    protected override object GetId() => ItemId;
}
