using Item_Trading_App_REST_API.Models.Trade;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.Trade
{
    public interface ITradeService
    {
        public Task<SentTradeOffer> CreateTradeOffer(CreateTradeOffer model);

        public Task<SentTradeOffer> GetSentTradeOffer(string tradeOfferId);

        public Task<SentRespondedTradeOffer> GetSentRespondedTradeOffer(string tradeOfferId);

        public Task<ReceivedTradeOffer> GetReceivedTradeOffer(string tradeOfferId);

        public Task<ReceivedRespondedTradeOffer> GetReceivedRespondedTradeOffer(string tradeOfferId);

        public Task<TradeOffersResult> GetSentTradeOffers(string userId);

        public Task<TradeOffersResult> GetSentRespondedTradeOffers(string userId);

        public Task<TradeOffersResult> GetReceivedTradeOffers(string userId);

        public Task<TradeOffersResult> GetReceivedRespondedTradeOffers(string userId);

        public Task<AcceptTradeOfferResult> AcceptTradeOffer(string tradeOfferId, string userId);

        public Task<RejectTradeOfferResult> RejectTradeOffer(string tradeOfferId, string userId);

        public Task<CancelTradeOfferResult> CancelTradeOffer(string tradeOfferId, string userId);
    }
}
