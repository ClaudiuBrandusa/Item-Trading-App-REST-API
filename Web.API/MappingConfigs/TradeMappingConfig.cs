using Application.Behaviors.Trade.CancelTrade;
using Application.Behaviors.Trade.CreateTrade;
using Application.Behaviors.Trade.RespondTrade;
using Application.Models.TradeItems;
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

        config.ForType<AcceptTradeOfferRequest, RespondTradeCommand>()
            .Map(dest => dest.UserId, src => MapContext.Current!.Parameters[nameof(RespondTradeCommand.UserId)].ToString())
            .Map(dest => dest.Response, src => MapContext.Current!.Parameters[nameof(RespondTradeCommand.Response)]);

        config.ForType<RejectTradeOfferRequest, RespondTradeCommand>()
            .Map(dest => dest.UserId, src => MapContext.Current!.Parameters[nameof(RespondTradeCommand.UserId)].ToString())
            .Map(dest => dest.Response, src => MapContext.Current!.Parameters[nameof(RespondTradeCommand.Response)]);

        config.ForType<CancelTradeOfferRequest, CancelTradeCommand>()
            .Map(dest => dest.UserId, src => MapContext.Current!.Parameters[nameof(RespondTradeCommand.UserId)].ToString());
    }
}
