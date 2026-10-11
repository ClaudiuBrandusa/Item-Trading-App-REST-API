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
    public Task<Result<TradeOfferResult>> CreateTradeOfferAsync(CreateTradeOfferCommand model, CancellationToken cancellationToken);

    /// <summary>
    /// Returns a trade offer
    /// </summary>
    public Task<Result<TradeOfferResult>> GetTradeOfferAsync(RequestTradeOfferQuery requestTradeOffer, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the trade offers
    /// </summary>
    public Task<Result<TradeOffersResult>> GetTradeOffersAsync(ListTradesQuery model, CancellationToken cancellationToken);

    /// <summary>
    /// Accepts the trade offer
    /// </summary>
    public Task<Result<TradeOfferResult>> AcceptTradeOfferAsync(RespondTradeCommand model, CancellationToken cancellationToken);

    /// <summary>
    /// Rejects the trade offer
    /// </summary>
    public Task<Result<TradeOfferResult>> RejectTradeOfferAsync(RespondTradeCommand model, CancellationToken cancellationToken);

    /// <summary>
    /// Cancels the trade offer
    /// </summary>
    public Task<Result<TradeOfferResult>> CancelTradeOfferAsync(CancelTradeCommand model, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the trade ids of the trades using the given item id
    /// </summary>
    public Task<Result<string[]>> GetItemTradeIdsAsync(GetTradesUsingTheItemQuery model, CancellationToken cancellationToken);

    /// <summary>
    /// Decides if the given item is used in any trade
    /// </summary>
    public Task<bool> IsItemUsedInTrade(ItemUsedInTradeQuery model, CancellationToken cancellationToken);
}
