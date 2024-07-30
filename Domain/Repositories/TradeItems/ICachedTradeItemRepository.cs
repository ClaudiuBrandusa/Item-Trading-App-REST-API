using Domain.Entities.Trades;

namespace Domain.Repositories.TradeItems;

public interface ICachedTradeItemRepository : ICachedRepository, IDisposable
{
    /// <summary>
    /// Will cache the result
    /// </summary>
    /// <param name="tradeId"></param>
    /// <param name="itemId"></param>
    /// <returns>Trade item with <paramref name="itemId"/> from the trade with <paramref name="tradeId"/></returns>
    Task<TradeItem?> GetTradeItemAsync(string tradeId, string itemId);

    /// <summary>
    /// Will cache the result. <paramref name="getItemNameFunc"/> must return the item name from the given item id
    /// </summary>
    /// <param name="tradeId"></param>
    /// <param name="getItemNameFunc"></param>
    /// <returns>Trade items from trade with <paramref name="tradeId"/></returns>
    Task<TradeItem[]> ListTradeItemsAsync(string tradeId);

    /// <summary>
    /// Will cache the result
    /// </summary>
    /// <param name="itemId"></param>
    /// <returns>Trade ids that have the item with <paramref name="itemId"/> as their content</returns>
    Task<string[]> GetTradeIdsUsingItemAsync(string itemId);

    /// <summary>
    /// Deletes the trade items of the trade with <paramref name="tradeId"/>
    /// </summary>
    /// <param name="tradeId"></param>
    /// <returns>Operation result</returns>
    Task<bool> DeleteTradeItemsAsync(string tradeId);
}
