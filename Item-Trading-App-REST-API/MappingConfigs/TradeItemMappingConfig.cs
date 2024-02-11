using Item_Trading_App_REST_API.Entities;
using Item_Trading_App_REST_API.Models.TradeItems;
using Item_Trading_App_REST_API.Resources.Commands.TradeItem;
using Mapster;

namespace Item_Trading_App_REST_API.MappingConfigs;

public class TradeItemMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.ForType<TradeItem, AddTradeItemCommand>()
            .Map(dest => dest.TradeId, src => MapContext.Current!.Parameters[nameof(AddTradeItemCommand.TradeId)].ToString());

        config.ForType<TradeContent, TradeItem>()
            .Map(dest => dest.Name, src => MapContext.Current!.Parameters[nameof(TradeItem.Name)].ToString());

        config.ForType<TradeContentHistory, TradeItem>()
            .Map(dest => dest.Name, src => src.ItemName);

        config.ForType<TradeItem, TradeContentHistory>()
            .Map(dest => dest.ItemName, src => src.Name)
            .Map(dest => dest.TradeId, src => MapContext.Current!.Parameters[nameof(TradeContentHistory.TradeId)].ToString());
    }
}
