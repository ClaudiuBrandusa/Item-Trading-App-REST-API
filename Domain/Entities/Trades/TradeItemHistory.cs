using Domain.Aggregates.Trades;

namespace Domain.Entities.Trades;

public class TradeItemHistory : Entity
{
    public string TradeId { get; private set; }

    public string ItemId { get; private set; }

    public string ItemName { get; private set; }

    public int Quantity { get; private set; }

    public int Price { get; private set; }

    public Trade Trade { get; private set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private TradeItemHistory() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public TradeItemHistory(string tradeId, string itemId, string itemName, int quantity, int price)
    {
        TradeId = tradeId;
        ItemId = itemId;
        ItemName = itemName;
        Quantity = quantity;
        Price = price;
    }

    protected override bool Compare(object obj)
    {
        var entity = obj as TradeItemHistory;

        if (entity is null) return false;

        return entity.TradeId == TradeId &&
               entity.ItemId == ItemId &&
               entity.ItemName == ItemName;
    }

    protected override object GetId() => $"{TradeId}{ItemId}";
}
