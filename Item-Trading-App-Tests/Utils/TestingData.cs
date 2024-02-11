using Item_Trading_App_REST_API.Models.TradeItems;
using Item_Trading_App_REST_API.Resources.Commands.TradeItem;

namespace Item_Trading_App_Tests.Utils;

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
            string itemName = $"item_name_{tradeItemId}";

            itemPrices.Add(tradeItemId, new TradeItem
            {
                ItemId = tradeItemId,
                Name = itemName,
                Price = 1,
                Quantity = 1
            });

            tradeItemRequests.Add(tradeItemId, new AddTradeItemCommand
            {
                ItemId = tradeItemId,
                Name = itemName,
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

    public static string GetTradeItemName(string tradeItemId)
    {
        return itemPrices.Where(x => x.Key == tradeItemId).FirstOrDefault().Value.Name;
    }

    public static AddTradeItemCommand[] GetTradeItemRequests(string[] tradeItemIds)
    {
        return tradeItemRequests.Where(x => tradeItemIds.Contains(x.Key)).Select(x => x.Value with { ItemId = Guid.NewGuid().ToString() }).ToArray();
    }
}
