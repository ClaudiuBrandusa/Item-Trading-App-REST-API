namespace Item_Trading_App_REST_API.Constants;

public static class CacheKeys
{
    public const string Trades = "trades:";

    public static class Identity
    {
        private const string ActiveUsers = "active_users:";

        public static string GetActiveUserKey(string userId) => $"{ActiveUsers}{userId}";
    }

    public static class Item
    {
        private const string Items = "items:";

        public static string GetItemsKey() => Items;

        public static string GetItemKey(string itemId) => $"{Items}{itemId}";
    }

    public static class Inventory
    {
        private const string InventoryKey = "inventory:";
        private const string InventoryItems = "inventory_items:";
        private const string InventoryLockedItem = "locked_amount:";

        public static string GetUserInventoryKey(string userId) => $"{InventoryKey}{userId}:{InventoryItems}";

        public static string GetAmountKey(string userId, string itemId) => $"{InventoryKey}{userId}:{InventoryItems}{itemId}";
    
        public static string GetLockedAmountKey(string userId, string itemId) => $"{InventoryKey}{userId}:{InventoryLockedItem}{itemId}";
    }

    public static class Trade
    {
        private const string TradeKey = "trade:";
        private const string SentTrades = "sent_trades:";
        private const string ReceivedTrades = "received_trades:";

        public static string GetTradeKey(string tradeId) => $"{Trades}{TradeKey}{tradeId}";

        public static string GetSentTradeKey(string userId, string tradeId) => $"{Trades}{SentTrades}{userId}+{tradeId}";

        public static string GetReceivedTradeKey(string userId, string tradeId) => $"{Trades}{ReceivedTrades}{userId}+{tradeId}";
    }

    public static class TradeItem
    {
        public const string TradeItemKey = "trade_item:";

        public static string GetTradeItemKey(string tradeId, string itemId) => $"{Trades}{tradeId}:{TradeItemKey}{itemId}";
    }

    public static class UsedItem
    {
        private const string UsedItems = "used_items:";

        public static string GetUsedItemKey(string itemId) => $"{UsedItems}{itemId}";
    }
}
