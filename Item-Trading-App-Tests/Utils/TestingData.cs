using Item_Trading_App_REST_API.Models.Item;
using Item_Trading_App_REST_API.Requests.TradeItem;

namespace Item_Trading_App_Tests.Utils;

public static class TestingData
{
    private static readonly Dictionary<string, ItemPrice> itemPrices = new();
    private static readonly Dictionary<string, AddTradeItemRequest> tradeItemRequests = new();
    public static readonly string DefaultTradeId = Guid.NewGuid().ToString();

    static TestingData()
    {
        string[] itemPriceIds = { "1", "2", "3", "4", "5" };

        foreach(string itemPriceId in itemPriceIds)
        {
            string itemName = $"item_name_{itemPriceId}";

            itemPrices.Add(itemPriceId, new ItemPrice
            {
                ItemId = itemPriceId,
                Name = itemName,
                Price = 1,
                Quantity = 1
            });

            tradeItemRequests.Add(itemPriceId, new AddTradeItemRequest
            {
                ItemId = itemPriceId,
                Name = itemName,
                Price = 1,
                Quantity = 1,
                TradeId = DefaultTradeId
            });
        }
    }

    public static List<ItemPrice> GetItemPrices(string[] itemPriceIds)
    {
        return itemPrices.Where(x => itemPriceIds.Contains(x.Key)).Select(x => x.Value).ToList();
    }

    public static AddTradeItemRequest[] GetTradeItemRequests(string[] itemPriceIds)
    {
        return tradeItemRequests.Where(x => itemPriceIds.Contains(x.Key)).Select(x => x.Value).ToArray();
    }
}
