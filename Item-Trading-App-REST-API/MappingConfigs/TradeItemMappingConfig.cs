using Item_Trading_App_REST_API.Entities;
using Item_Trading_App_REST_API.Models.Item;
using Item_Trading_App_REST_API.Resources.Commands.TradeItem;
using Mapster;

namespace Item_Trading_App_REST_API.MappingConfigs;

public class TradeItemMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.ForType<ItemPrice, AddTradeItemCommand>()
            .Map(dest => dest.TradeId, src => MapContext.Current!.Parameters[nameof(AddTradeItemCommand.TradeId)].ToString());

        config.ForType<TradeContent, ItemPrice>()
            .Map(dest => dest.Name, src => MapContext.Current!.Parameters[nameof(ItemPrice.Name)].ToString());
    }
}
