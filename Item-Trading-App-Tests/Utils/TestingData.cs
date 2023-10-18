using Item_Trading_App_REST_API.Models.Item;

namespace Item_Trading_App_Tests.Utils;

public static class TestingData
{
    private static readonly Dictionary<string, ItemPrice> itemPrices = new();

    static TestingData()
    {
        string[] itemPriceIds = { "1", "2", "3", "4", "5" };

        foreach(string itemPriceId in itemPriceIds)
        {
            itemPrices.Add(itemPriceId, new ItemPrice
            {
                ItemId = itemPriceId,
                Name = $"item_name_{itemPriceId}",
                Price = 1,
                Quantity = 1
            });
        }
    }

    public static List<ItemPrice> GetItemPrices(string[] itemPriceIds)
    {
        return itemPrices.Where(x => itemPriceIds.Contains(x.Key)).Select(x => x.Value).ToList();
    }
}
