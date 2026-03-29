using Application.Behaviors.Trade.CancelTrade;
using Application.Behaviors.Trade.CreateTrade;
using Application.Behaviors.Trade.GetTrade;
using Application.Behaviors.Trade.GetTradeItemIds;
using Application.Behaviors.Trade.ItemUsedInTrade;
using Application.Behaviors.Trade.ListTrades;
using Application.Behaviors.Trade.RespondTrade;
using Application.Results.Trades;

namespace Application.Services.Trades;

public interface ITradeService
{
    /// <summary>
    /// Creates the trade offer
    /// </summary>
    public Task<TradeOfferResult> CreateTradeOfferAsync(CreateTradeOfferCommand model);

    /// <summary>
    /// Returns a trade offer
    /// </summary>
    public Task<TradeOfferResult> GetTradeOfferAsync(RequestTradeOfferQuery requestTradeOffer);

    /// <summary>
    /// Returns the trade offers
    /// </summary>
    public Task<TradeOffersResult> GetTradeOffersAsync(ListTradesQuery model);

    /// <summary>
    /// Accepts the trade offer
    /// </summary>
    public Task<TradeOfferResult> AcceptTradeOfferAsync(RespondTradeCommand model);

    /// <summary>
    /// Rejects the trade offer
    /// </summary>
    public Task<TradeOfferResult> RejectTradeOfferAsync(RespondTradeCommand model);

    /// <summary>
    /// Cancels the trade offer
    /// </summary>
    public Task<TradeOfferResult> CancelTradeOfferAsync(CancelTradeCommand model);

    /// <summary>
    /// Retrieves the trade ids of the trades using the given item id
    /// </summary>
    public Task<string[]> GetItemTradeIdsAsync(GetTradesUsingTheItemQuery model);

    /// <summary>
    /// Decides if the given item is used in any trade
    /// </summary>
    public Task<bool> IsItemUsedInTrade(ItemUsedInTradeQuery model);
}
