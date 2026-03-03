using Domain.Aggregates.Trades;
using Domain.Entities.Items;
using Domain.Primitives;

namespace Domain.Entities.Trades;

public class TradeItem : Entity
{
    public string TradeId { get; private set; }

    public string ItemId { get; private set; }

    public int Quantity { get; private set; }

    public int Price { get; private set; }

    public Item Item { get; private set; }

    public virtual Trade Trade { get; private set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private TradeItem() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public TradeItem(string tradeId, string itemId, int quantity, int price)
    {
        TradeId = tradeId;
        ItemId = itemId;
        Quantity = quantity;
        Price = price;
    }

    public void UpdateQuantity(int quantity) => Quantity = quantity;

    public void UpdatePrice(int price) => Price = price;

    protected override bool Compare(object obj)
    {
        if (obj is not TradeItem entity) return false;

        return entity.TradeId == TradeId &&
               entity.ItemId == ItemId &&
               entity.Quantity == Quantity &&
               entity.Price == Price;
    }

    protected override object GetId() => $"{TradeId}{ItemId}";
}
