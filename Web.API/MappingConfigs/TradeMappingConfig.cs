using Application.Behaviors.Trade.CancelTrade;
using Application.Behaviors.Trade.CreateTrade;
using Application.Behaviors.Trade.GetTrade;
using Application.Behaviors.Trade.ListTrades;
using Application.Behaviors.Trade.RespondTrade;
using Application.Models.TradeItems;
using Application.Models.Trades;
using Application.Results.Trades;
using Item_Trading_App_Contracts.Base.Item;
using Item_Trading_App_Contracts.Requests.Trade;
using Item_Trading_App_Contracts.Responses.Trade;
using Mapster;
using System.Linq;

namespace Item_Trading_App_REST_API.MappingConfigs;

public class TradeMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.ForType<TradeOfferResult, TradeOfferSuccessResponse>()
            .Map(dest => dest.Items, src => src.Items.Select(item => new ItemWithPrice { Id = item.ItemId, Name = item.ItemName, Price = item.Price, Quantity = item.Quantity }));

        config.ForType<TradeOfferRequest, CreateTradeOfferCommand>()
            .Map(dest => dest.SenderUserId, src => MapContext.Current!.Parameters[nameof(CreateTradeOfferCommand.SenderUserId)].ToString())
            .Map(dest => dest.Items, src => src.Items.Select(item => new TradeItemDTO
            {
                ItemId = item.Id,
                ItemName = item.Name,
                Quantity = item.Quantity,
                Price = item.Price
            }));

        config.ForType<string, TradeOfferFailedResponse>()
            .MapWith(str => new TradeOfferFailedResponse
            {
                Errors = new string[] { str }
            });

        config.ForType<string, RequestTradeOfferQuery>()
            .MapWith(str =>
                new RequestTradeOfferQuery { TradeId = str });

        config.ForType<string, ListTradesQuery>()
            .MapWith(str => BuildListTradesQuery(str));

        config.ForType<AcceptTradeOfferRequest, RespondTradeCommand>()
            .Map(dest => dest.UserId, src => MapContext.Current!.Parameters[nameof(RespondTradeCommand.UserId)].ToString())
            .Map(dest => dest.Response, src => MapContext.Current!.Parameters[nameof(RespondTradeCommand.Response)]);

        config.ForType<string, AcceptTradeOfferFailedResponse>()
            .MapWith(str => new AcceptTradeOfferFailedResponse
            {
                Errors = new string[] { str }
            });

        config.ForType<RejectTradeOfferRequest, RespondTradeCommand>()
            .Map(dest => dest.UserId, src => MapContext.Current!.Parameters[nameof(RespondTradeCommand.UserId)].ToString())
            .Map(dest => dest.Response, src => MapContext.Current!.Parameters[nameof(RespondTradeCommand.Response)]);

        config.ForType<string, RejectTradeOfferFailedResponse>()
            .MapWith(str => new RejectTradeOfferFailedResponse
            {
                Errors = new string[] { str }
            });

        config.ForType<CancelTradeOfferRequest, CancelTradeCommand>()
            .Map(dest => dest.UserId, src => MapContext.Current!.Parameters[nameof(RespondTradeCommand.UserId)].ToString());

        config.ForType<string, CancelTradeOfferFailedResponse>()
            .MapWith(str => new CancelTradeOfferFailedResponse
            {
                Errors = new string[] { str }
            });
    }

    private static ListTradesQuery BuildListTradesQuery(string userId)
    {
        MapContext.Current!.Parameters.TryGetValue(nameof(ListTradesQuery.Responded), out var responded);
        MapContext.Current!.Parameters.TryGetValue(nameof(ListTradesQuery.TradeDirection), out var tradeDirection);

        return new ListTradesQuery
        {
            UserId = userId,
            TradeItemIds = (MapContext.Current!.Parameters[nameof(ListTradesQuery.TradeItemIds)]! as string[])!,
            Responded = responded is not null && bool.Parse(responded.ToString()!),
            TradeDirection = tradeDirection is null ? TradeDirection.All : (TradeDirection) tradeDirection
        };
    }
}
