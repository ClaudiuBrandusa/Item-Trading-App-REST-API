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
    public Task<SentTradeOfferResult> CreateTradeOffer(CreateTradeOfferCommand model);

    /// <summary>
    /// Returns a sent trade offer
    /// </summary>
    public Task<SentTradeOfferResult> GetSentTradeOffer(RequestTradeOfferQuery requestTradeOffer);

    /// <summary>
    /// Returns a responded sent trade offer
    /// </summary>
    public Task<RespondedSentTradeOfferResult> GetRespondedSentTradeOffer(RequestTradeOfferQuery requestTradeOffer);

    /// <summary>
    /// Returns a received trade offer
    /// </summary>
    public Task<ReceivedTradeOfferResult> GetReceivedTradeOffer(RequestTradeOfferQuery requestTradeOffer);

    /// <summary>
    /// Returns a responded received trade offer
    /// </summary>
    public Task<RespondedReceivedTradeOfferResult> GetReceivedRespondedTradeOffer(RequestTradeOfferQuery requestTradeOffer);

    /// <summary>
    /// Returns all of the sent trade offers
    /// </summary>
    public Task<TradeOffersResult> GetSentTradeOffers(ListTradesQuery model);

    /// <summary>
    /// Returns all of the responded sent trade offers
    /// </summary>
    public Task<TradeOffersResult> GetSentRespondedTradeOffers(ListTradesQuery model);

    /// <summary>
    /// Returns all of the received trade offers
    /// </summary>
    public Task<TradeOffersResult> GetReceivedTradeOffers(ListTradesQuery model);

    /// <summary>
    /// Returns all of the responded received trade offers
    /// </summary>
    public Task<TradeOffersResult> GetReceivedRespondedTradeOffers(ListTradesQuery model);

    /// <summary>
    /// Accepts the trade offer
    /// </summary>
    public Task<RespondedTradeOfferResult> AcceptTradeOffer(RespondTradeCommand model);

    /// <summary>
    /// Rejects the trade offer
    /// </summary>
    public Task<RespondedTradeOfferResult> RejectTradeOffer(RespondTradeCommand model);

    /// <summary>
    /// Cancels the trade offer
    /// </summary>
    public Task<CancelTradeOfferResult> CancelTradeOffer(CancelTradeCommand model);
}
