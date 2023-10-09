using Item_Trading_App_REST_API.Models.Trade;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.Trade;

public interface ITradeService
{
    /// <summary>
    /// Creates the trade offer
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public Task<SentTradeOffer> CreateTradeOffer(CreateTradeOffer model);

    /// <summary>
    /// Returns a sent trade offer
    /// </summary>
    /// <param name="tradeOfferId"></param>
    /// <returns></returns>
    public Task<SentTradeOffer> GetSentTradeOffer(RequestTradeOffer requestTradeOffer);

    /// <summary>
    /// Returns a responded sent trade offer
    /// </summary>
    /// <param name="tradeOfferId"></param>
    /// <returns></returns>
    public Task<SentRespondedTradeOffer> GetSentRespondedTradeOffer(RequestTradeOffer requestTradeOffer);

    /// <summary>
    /// Returns a received trade offer
    /// </summary>
    /// <param name="tradeOfferId"></param>
    /// <returns></returns>
    public Task<ReceivedTradeOffer> GetReceivedTradeOffer(RequestTradeOffer requestTradeOffer);

    /// <summary>
    /// Returns a responded received trade offer
    /// </summary>
    /// <param name="tradeOfferId"></param>
    /// <returns></returns>
    public Task<ReceivedRespondedTradeOffer> GetReceivedRespondedTradeOffer(RequestTradeOffer requestTradeOffer);

    /// <summary>
    /// Returns all of the sent trade offers
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public Task<TradeOffersResult> GetSentTradeOffers(string userId);

    /// <summary>
    /// Returns all of the responded sent trade offers
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public Task<TradeOffersResult> GetSentRespondedTradeOffers(string userId);

    /// <summary>
    /// Returns all of the received trade offers
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public Task<TradeOffersResult> GetReceivedTradeOffers(string userId);

    /// <summary>
    /// Returns all of the responded received trade offers
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public Task<TradeOffersResult> GetReceivedRespondedTradeOffers(string userId);

    /// <summary>
    /// Accepts the trade offer
    /// </summary>
    /// <param name="tradeOfferId"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    public Task<AcceptTradeOfferResult> AcceptTradeOffer(string tradeOfferId, string userId);

    /// <summary>
    /// Rejects the trade offer
    /// </summary>
    /// <param name="tradeOfferId"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    public Task<RejectTradeOfferResult> RejectTradeOffer(string tradeOfferId, string userId);

    /// <summary>
    /// Cancels the trade offer
    /// </summary>
    /// <param name="tradeOfferId"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    public Task<CancelTradeOfferResult> CancelTradeOffer(string tradeOfferId, string userId);
}
