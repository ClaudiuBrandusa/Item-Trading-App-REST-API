using Domain.Entities.Trades;

namespace Domain.Repositories.TradeItems;

public interface ITradeItemRepository : IRepository, IDisposable
{
    /// <param name="tradeId"></param>
    /// <param name="itemId"></param>
    /// <returns>Trade item with <paramref name="itemId"/> from the trade with <paramref name="tradeId"/></returns>
    Task<TradeItem?> GetTradeItemAsync(string tradeId, string itemId);

    /// <param name="tradeId"></param>
    /// <returns>Trade items from trade with <paramref name="tradeId"/></returns>
    Task<TradeItem[]> ListTradeItemsAsync(string tradeId);

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
