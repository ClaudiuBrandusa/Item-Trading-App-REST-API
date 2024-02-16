using Item_Trading_App_REST_API.Models.Trade;
using Item_Trading_App_REST_API.Resources.Commands.Trade;
using Item_Trading_App_REST_API.Resources.Queries.Trade;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.Trade;

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
    /// Returns all of the sent trade offers
    /// </summary>
    public Task<TradeOffersResult> GetSentTradeOffersAsync(ListTradesQuery model);

    /// <summary>
    /// Returns all of the received trade offers
    /// </summary>
    public Task<TradeOffersResult> GetReceivedTradeOffersAsync(ListTradesQuery model);

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
