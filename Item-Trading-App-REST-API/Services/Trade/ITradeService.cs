﻿using Item_Trading_App_REST_API.Models.Trade;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.Trade;

public interface ITradeService
{
    /// <summary>
    /// Creates the trade offer
    /// </summary>
    public Task<SentTradeOffer> CreateTradeOffer(CreateTradeOffer model);

    /// <summary>
    /// Returns a sent trade offer
    /// </summary>
    public Task<SentTradeOffer> GetSentTradeOffer(RequestTradeOffer requestTradeOffer);

    /// <summary>
    /// Returns a responded sent trade offer
    /// </summary>
    public Task<SentRespondedTradeOffer> GetSentRespondedTradeOffer(RequestTradeOffer requestTradeOffer);

    /// <summary>
    /// Returns a received trade offer
    /// </summary>
    public Task<ReceivedTradeOffer> GetReceivedTradeOffer(RequestTradeOffer requestTradeOffer);

    /// <summary>
    /// Returns a responded received trade offer
    /// </summary>
    public Task<ReceivedRespondedTradeOffer> GetReceivedRespondedTradeOffer(RequestTradeOffer requestTradeOffer);

    /// <summary>
    /// Returns all of the sent trade offers
    /// </summary>
    public Task<TradeOffersResult> GetSentTradeOffers(string userId);

    /// <summary>
    /// Returns all of the responded sent trade offers
    /// </summary>
    public Task<TradeOffersResult> GetSentRespondedTradeOffers(string userId);

    /// <summary>
    /// Returns all of the received trade offers
    /// </summary>
    public Task<TradeOffersResult> GetReceivedTradeOffers(string userId);

    /// <summary>
    /// Returns all of the responded received trade offers
    /// </summary>
    public Task<TradeOffersResult> GetReceivedRespondedTradeOffers(string userId);

    /// <summary>
    /// Accepts the trade offer
    /// </summary>
    public Task<AcceptTradeOfferResult> AcceptTradeOffer(RespondTrade model);

    /// <summary>
    /// Rejects the trade offer
    /// </summary>
    public Task<RejectTradeOfferResult> RejectTradeOffer(RespondTrade model);

    /// <summary>
    /// Cancels the trade offer
    /// </summary>
    public Task<CancelTradeOfferResult> CancelTradeOffer(RespondTrade model);
}
