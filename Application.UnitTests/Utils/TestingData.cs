using Application.Behaviors.TradeItem.AddTradeItem;
using Domain.Entities.Trades;

namespace Application_UnitTests.Utils;

public static class TestingData
{
    private static readonly Dictionary<string, TradeItem> itemPrices = new();
    private static readonly Dictionary<string, AddTradeItemCommand> tradeItemRequests = new();
    public static readonly string DefaultTradeId = Guid.NewGuid().ToString();

    static TestingData()
    {
        string[] tradeItemIds = { "1", "2", "3", "4", "5" };

        foreach(string tradeItemId in tradeItemIds)
        {
            itemPrices.Add(tradeItemId, new TradeItem(DefaultTradeId, tradeItemId, 1, 1));

            tradeItemRequests.Add(tradeItemId, new AddTradeItemCommand
            {
                ItemId = tradeItemId,
                Price = 1,
                Quantity = 1,
                TradeId = DefaultTradeId
            });
        }
    }

    public static TradeItem[] GetTradeItems(string[] tradeItemIds)
    {
        return itemPrices.Where(x => tradeItemIds.Contains(x.Key)).Select(x => x.Value).ToArray();
    }

    public static AddTradeItemCommand[] GetTradeItemRequests(string[] tradeItemIds)
    {
        return tradeItemRequests.Where(x => tradeItemIds.Contains(x.Key)).Select(x => x.Value with { ItemId = Guid.NewGuid().ToString() }).ToArray();
    }
}
