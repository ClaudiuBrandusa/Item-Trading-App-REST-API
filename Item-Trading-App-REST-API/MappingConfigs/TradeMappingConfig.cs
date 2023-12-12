using Item_Trading_App_Contracts.Base.Item;
using Item_Trading_App_Contracts.Requests.Trade;
using Item_Trading_App_Contracts.Responses.Trade;
using Item_Trading_App_REST_API.Models.Item;
using Item_Trading_App_REST_API.Models.Trade;
using Mapster;
using System.Linq;

namespace Item_Trading_App_REST_API.MappingConfigs;

public class TradeMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.ForType<string, RequestTradeOffer>()
            .MapWith(str =>
                new RequestTradeOffer { TradeOfferId = str, UserId = MapContext.Current!.Parameters[nameof(RequestTradeOffer.UserId)].ToString() });

        config.ForType<SentTradeOffer, GetSentTradeOfferSuccessResponse>()
            .Map(dest => dest.TradeId, src => src.TradeOfferId)
            .Map(dest => dest.Items, src => src.Items.Select(i => new ItemWithPrice { Id = i.ItemId, Name = i.Name, Price = i.Price, Quantity = i.Quantity }));

        config.ForType<SentRespondedTradeOffer, GetSentRespondedTradeOfferSuccessResponse>()
            .Map(dest => dest.TradeId, src => src.TradeOfferId)
            .Map(dest => dest.Items, src => src.Items.Select(i => new ItemWithPrice { Id = i.ItemId, Name = i.Name, Price = i.Price, Quantity = i.Quantity }));

        config.ForType<ReceivedTradeOffer, GetReceivedTradeOfferSuccessResponse>()
            .Map(dest => dest.TradeId, src => src.TradeOfferId)
            .Map(dest => dest.Items, src => src.Items.Select(i => new ItemWithPrice { Id = i.ItemId, Name = i.Name, Price = i.Price, Quantity = i.Quantity }));

        config.ForType<ReceivedRespondedTradeOffer, GetReceivedRespondedTradeOfferSuccessResponse>()
            .Map(dest => dest.TradeId, src => src.TradeOfferId)
            .Map(dest => dest.Items, src => src.Items.Select(i => new ItemWithPrice { Id = i.ItemId, Name = i.Name, Price = i.Price, Quantity = i.Quantity }));

        config.ForType<TradeOffersResult, ListTradeOffersSuccessResponse>()
            .Map(dest => dest.TradeOffersIds, src => src.TradeOffers);

        config.ForType<TradeOfferRequest, CreateTradeOffer>()
            .Map(dest => dest.SenderUserId, src => MapContext.Current!.Parameters[nameof(CreateTradeOffer.SenderUserId)].ToString())
            .Map(dest => dest.Items, src => src.Items.Select(t => new ItemPrice { ItemId = t.Id, Price = t.Price, Quantity = t.Quantity }));

        config.ForType<AcceptTradeOfferRequest, RespondTrade>()
            
            .Map(dest => dest.UserId, src => MapContext.Current!.Parameters[nameof(RespondTrade.UserId)].ToString());

        config.ForType<RejectTradeOfferRequest, RespondTrade>()
            .Map(dest => dest.UserId, src => MapContext.Current!.Parameters[nameof(RespondTrade.UserId)].ToString());

        config.ForType<RejectTradeOfferResult, RejectTradeOfferSuccessResponse>()
            .Map(dest => dest.Id, src => src.TradeOfferId);

        config.ForType<CancelTradeOfferRequest, RespondTrade>()
            .Map(dest => dest.UserId, src => MapContext.Current!.Parameters[nameof(RespondTrade.UserId)].ToString());
    }
}
