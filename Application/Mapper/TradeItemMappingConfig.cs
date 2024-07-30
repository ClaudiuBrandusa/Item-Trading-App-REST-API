using Application.Behaviors.TradeItem.AddTradeItem;
using Application.Models.TradeItems;
using Domain.Entities.Trades;
using Mapster;

namespace Application.Mapper;

public class TradeItemMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.ForType<TradeItem, TradeItemDTO>()
            .Map(dest => dest.ItemName, src => MapContext.Current!.Parameters[nameof(TradeItemDTO.ItemName)].ToString());

        config.ForType<AddTradeItemCommand, TradeItem>()
            .MapWith((AddTradeItemCommand command) => new TradeItem(command.TradeId, command.ItemId, command.Quantity, command.Price));

        config.ForType<TradeItemHistory, TradeItem>()
            .MapWith((TradeItemHistory entity) => new TradeItem
            (
                entity.TradeId,
                entity.ItemId,
                entity.Quantity,
                entity.Price
            ));

        config.ForType<TradeItem, TradeItemHistory>()
            .MapWith((TradeItem tradeItem) => new TradeItemHistory(
                MapContext.Current!.Parameters[nameof(TradeItemHistory.TradeId)].ToString() ?? string.Empty,
                tradeItem.ItemId,
                MapContext.Current!.Parameters[nameof(TradeItemHistory.ItemName)].ToString() ?? string.Empty,
                tradeItem.Quantity,
                tradeItem.Price
            ));

        config.ForType<CachedTradeItem, TradeItem>()
            .MapWith((CachedTradeItem cachedEntity) => new TradeItem
            (
                cachedEntity.TradeId,
                cachedEntity.ItemId,
                cachedEntity.Quantity,
                cachedEntity.Price
            ));
    }
}
