using Application.Behaviors.Trade.CancelTrade;
using Application.Behaviors.Trade.CreateTrade;
using Application.Behaviors.Trade.GetTrade;
using Application.Behaviors.Trade.ListTrades;
using Application.Behaviors.Trade.RespondTrade;
using Application.Models.Trade;

namespace Application.Services.Trade;

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
}
