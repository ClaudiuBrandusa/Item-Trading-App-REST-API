using Item_Trading_App_Contracts.Base.Item;
using Item_Trading_App_Contracts.Requests.Trade;
using Item_Trading_App_Contracts.Responses.Trade;
using Item_Trading_App_REST_API.Models.Trade;
using Item_Trading_App_REST_API.Models.TradeItems;
using Item_Trading_App_REST_API.Resources.Commands.Trade;
using Item_Trading_App_REST_API.Resources.Queries.Trade;
using Mapster;
using System.Linq;

namespace Item_Trading_App_REST_API.MappingConfigs;

public class TradeMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.ForType<string, RequestSentTradeOfferQuery>()
            .MapWith(str =>
                new RequestSentTradeOfferQuery { TradeOfferId = str, UserId = MapContext.Current!.Parameters[nameof(RequestTradeOfferQuery.UserId)].ToString() });

        config.ForType<string, RequestRespondedSentTradeOfferQuery>()
            .MapWith(str =>
                new RequestRespondedSentTradeOfferQuery { TradeOfferId = str, UserId = MapContext.Current!.Parameters[nameof(RequestTradeOfferQuery.UserId)].ToString() });

        config.ForType<string, RequestReceivedTradeOfferQuery>()
            .MapWith(str =>
                new RequestReceivedTradeOfferQuery { TradeOfferId = str, UserId = MapContext.Current!.Parameters[nameof(RequestTradeOfferQuery.UserId)].ToString() });

        config.ForType<string, RequestRespondedReceivedTradeOfferQuery>()
            .MapWith(str =>
                new RequestRespondedReceivedTradeOfferQuery { TradeOfferId = str, UserId = MapContext.Current!.Parameters[nameof(RequestTradeOfferQuery.UserId)].ToString() });

        config.ForType<string, ListSentTradesQuery>()
            .MapWith(str =>
                new ListSentTradesQuery { UserId = str, TradeItemIds = MapContext.Current!.Parameters[nameof(ListTradesQuery.TradeItemIds)] as string[] });

        config.ForType<string, ListRespondedSentTradesQuery>()
            .MapWith(str =>
                new ListRespondedSentTradesQuery { UserId = str, TradeItemIds = MapContext.Current!.Parameters[nameof(ListTradesQuery.TradeItemIds)] as string[] });

        config.ForType<string, ListReceivedTradesQuery>()
            .MapWith(str =>
                new ListReceivedTradesQuery { UserId = str, TradeItemIds = MapContext.Current!.Parameters[nameof(ListTradesQuery.TradeItemIds)] as string[] });

        config.ForType<string, ListRespondedReceivedTradesQuery>()
            .MapWith(str =>
                new ListRespondedReceivedTradesQuery { UserId = str, TradeItemIds = MapContext.Current!.Parameters[nameof(ListTradesQuery.TradeItemIds)] as string[] });
        
        config.ForType<SentTradeOfferResult, GetSentTradeOfferSuccessResponse>()
            .Map(dest => dest.TradeId, src => src.TradeOfferId)
            .Map(dest => dest.Items, src => src.Items.Select(i => new ItemWithPrice { Id = i.ItemId, Name = i.Name, Price = i.Price, Quantity = i.Quantity }));

        config.ForType<RespondedSentTradeOfferResult, GetSentRespondedTradeOfferSuccessResponse>()
            .Map(dest => dest.TradeId, src => src.TradeOfferId)
            .Map(dest => dest.Items, src => src.Items.Select(i => new ItemWithPrice { Id = i.ItemId, Name = i.Name, Price = i.Price, Quantity = i.Quantity }));

        config.ForType<ReceivedTradeOfferResult, GetReceivedTradeOfferSuccessResponse>()
            .Map(dest => dest.TradeId, src => src.TradeOfferId)
            .Map(dest => dest.Items, src => src.Items.Select(i => new ItemWithPrice { Id = i.ItemId, Name = i.Name, Price = i.Price, Quantity = i.Quantity }));

        config.ForType<RespondedReceivedTradeOfferResult, GetReceivedRespondedTradeOfferSuccessResponse>()
            .Map(dest => dest.TradeId, src => src.TradeOfferId)
            .Map(dest => dest.Items, src => src.Items.Select(i => new ItemWithPrice { Id = i.ItemId, Name = i.Name, Price = i.Price, Quantity = i.Quantity }));

        config.ForType<TradeOffersResult, ListTradeOffersSuccessResponse>()
            .Map(dest => dest.TradeOffersIds, src => src.TradeOffers);

        config.ForType<TradeOfferRequest, CreateTradeOfferCommand>()
            .Map(dest => dest.SenderUserId, src => MapContext.Current!.Parameters[nameof(CreateTradeOfferCommand.SenderUserId)].ToString())
            .Map(dest => dest.Items, src => src.Items.Select(t => new TradeItem { ItemId = t.Id, Price = t.Price, Quantity = t.Quantity }));

        config.ForType<AcceptTradeOfferRequest, RespondTradeCommand>()
            .Map(dest => dest.UserId, src => MapContext.Current!.Parameters[nameof(RespondTradeCommand.UserId)].ToString())
            .Map(dest => dest.Response, src => MapContext.Current!.Parameters[nameof(RespondTradeCommand.Response)]);

        config.ForType<RejectTradeOfferRequest, RespondTradeCommand>()
            .Map(dest => dest.UserId, src => MapContext.Current!.Parameters[nameof(RespondTradeCommand.UserId)].ToString())
            .Map(dest => dest.Response, src => MapContext.Current!.Parameters[nameof(RespondTradeCommand.Response)]);

        config.ForType<RejectTradeOfferResult, RejectTradeOfferSuccessResponse>()
            .Map(dest => dest.Id, src => src.TradeOfferId);

        config.ForType<CancelTradeOfferRequest, CancelTradeCommand>()
            .Map(dest => dest.UserId, src => MapContext.Current!.Parameters[nameof(RespondTradeCommand.UserId)].ToString());
    }
}
