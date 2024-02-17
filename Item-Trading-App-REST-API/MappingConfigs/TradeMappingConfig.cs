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
        config.ForType<string, RequestTradeOfferQuery>()
            .MapWith(str =>
                new RequestTradeOfferQuery { TradeId = str });

        config.ForType<string, ListTradesQuery>()
            .MapWith(str => BuildListTradesQuery(str));

        config.ForType<TradeOfferResult, TradeOfferSuccessResponse>()
            .Map(dest => dest.Items, src => src.Items.Select(i => new ItemWithPrice { Id = i.ItemId, Name = i.Name, Price = i.Price, Quantity = i.Quantity }));

        config.ForType<TradeOfferRequest, CreateTradeOfferCommand>()
            .Map(dest => dest.SenderUserId, src => MapContext.Current!.Parameters[nameof(CreateTradeOfferCommand.SenderUserId)].ToString())
            .Map(dest => dest.Items, src => src.Items.Select(t => new TradeItem { ItemId = t.Id, Price = t.Price, Quantity = t.Quantity }));

        config.ForType<AcceptTradeOfferRequest, RespondTradeCommand>()
            .Map(dest => dest.UserId, src => MapContext.Current!.Parameters[nameof(RespondTradeCommand.UserId)].ToString())
            .Map(dest => dest.Response, src => MapContext.Current!.Parameters[nameof(RespondTradeCommand.Response)]);

        config.ForType<RejectTradeOfferRequest, RespondTradeCommand>()
            .Map(dest => dest.UserId, src => MapContext.Current!.Parameters[nameof(RespondTradeCommand.UserId)].ToString())
            .Map(dest => dest.Response, src => MapContext.Current!.Parameters[nameof(RespondTradeCommand.Response)]);

        config.ForType<CancelTradeOfferRequest, CancelTradeCommand>()
            .Map(dest => dest.UserId, src => MapContext.Current!.Parameters[nameof(RespondTradeCommand.UserId)].ToString());
    }

    private static ListTradesQuery BuildListTradesQuery(string userId)
    {
        MapContext.Current!.Parameters.TryGetValue(nameof(ListTradesQuery.Responded), out object responded);
        MapContext.Current!.Parameters.TryGetValue(nameof(ListTradesQuery.TradeDirection), out object tradeDirection);

        return new ListTradesQuery
        {
            UserId = userId,
            TradeItemIds = MapContext.Current!.Parameters[nameof(ListTradesQuery.TradeItemIds)] as string[],
            Responded = responded is not null && bool.Parse(responded.ToString()),
            TradeDirection = tradeDirection is null ? TradeDirection.All : (TradeDirection) tradeDirection
        };
    }
}
