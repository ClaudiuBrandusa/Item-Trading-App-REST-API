using Domain.TradeItems;
using Domain.Trades;

namespace Domain.Repositories;

public interface ITradeItemRepository : IRepository, IDisposable
{
    /// <summary>
    /// Will cache the result
    /// </summary>
    /// <param name="tradeId"></param>
    /// <param name="itemId"></param>
    /// <returns>Trade item with <paramref name="itemId"/> from the trade with <paramref name="tradeId"/></returns>
    Task<TradeContent?> GetTradeContentCachedAsync(string tradeId, string itemId);

    /// <param name="tradeId"></param>
    /// <param name="itemId"></param>
    /// <returns>Trade item with <paramref name="itemId"/> from the trade with <paramref name="tradeId"/></returns>
    Task<TradeContent?> GetTradeContentAsync(string tradeId, string itemId);

    /// <param name="tradeId"></param>
    /// <returns>Trade items from trade with <paramref name="tradeId"/></returns>
    Task<TradeContent[]> ListTradeContentsAsync(string tradeId);

    /// <summary>
    /// Will cache the result. <paramref name="getItemNameFunc"/> must return the item name from the given item id
    /// </summary>
    /// <param name="tradeId"></param>
    /// <param name="getItemNameFunc"></param>
    /// <returns>Trade items from trade with <paramref name="tradeId"/></returns>
    Task<TradeItem[]> ListTradeItemsCachedAsync(string tradeId, Func<string, Task<string>> getItemNameFunc);

    /// <param name="itemId"></param>
    /// <returns>Trade ids that have the item with <paramref name="itemId"/> as their content</returns>
    Task<string[]> GetTradeIdsUsingItemAsync(string itemId);

    /// <summary>
    /// Will cache the result
    /// </summary>
    /// <param name="itemId"></param>
    /// <returns>Trade ids that have the item with <paramref name="itemId"/> as their content</returns>
    Task<string[]> GetTradeIdsUsingItemCachedAsync(string itemId);

    /// <summary>
    /// Deletes the trade items of the trade with <paramref name="tradeId"/>
    /// </summary>
    /// <param name="tradeId"></param>
    /// <returns>Operation result</returns>
    Task<bool> DeleteTradeItemsAsync(string tradeId);
}
