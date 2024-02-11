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
    public Task<SentTradeOfferResult> CreateTradeOfferAsync(CreateTradeOfferCommand model);

    /// <summary>
    /// Returns a sent trade offer
    /// </summary>
    public Task<SentTradeOfferResult> GetSentTradeOfferAsync(RequestTradeOfferQuery requestTradeOffer);

    /// <summary>
    /// Returns a responded sent trade offer
    /// </summary>
    public Task<RespondedSentTradeOfferResult> GetRespondedSentTradeOfferAsync(RequestTradeOfferQuery requestTradeOffer);

    /// <summary>
    /// Returns a received trade offer
    /// </summary>
    public Task<ReceivedTradeOfferResult> GetReceivedTradeOfferAsync(RequestTradeOfferQuery requestTradeOffer);

    /// <summary>
    /// Returns a responded received trade offer
    /// </summary>
    public Task<RespondedReceivedTradeOfferResult> GetReceivedRespondedTradeOfferAsync(RequestTradeOfferQuery requestTradeOffer);

    /// <summary>
    /// Returns all of the sent trade offers
    /// </summary>
    public Task<TradeOffersResult> GetSentTradeOffersAsync(ListTradesQuery model);

    /// <summary>
    /// Returns all of the responded sent trade offers
    /// </summary>
    public Task<TradeOffersResult> GetSentRespondedTradeOffersAsync(ListTradesQuery model);

    /// <summary>
    /// Returns all of the received trade offers
    /// </summary>
    public Task<TradeOffersResult> GetReceivedTradeOffersAsync(ListTradesQuery model);

    /// <summary>
    /// Returns all of the responded received trade offers
    /// </summary>
    public Task<TradeOffersResult> GetReceivedRespondedTradeOffersAsync(ListTradesQuery model);

    /// <summary>
    /// Accepts the trade offer
    /// </summary>
    public Task<RespondedTradeOfferResult> AcceptTradeOfferAsync(RespondTradeCommand model);

    /// <summary>
    /// Rejects the trade offer
    /// </summary>
    public Task<RespondedTradeOfferResult> RejectTradeOfferAsync(RespondTradeCommand model);

    /// <summary>
    /// Cancels the trade offer
    /// </summary>
    public Task<CancelTradeOfferResult> CancelTradeOfferAsync(CancelTradeCommand model);
}
