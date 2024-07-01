using Application.Behaviors.Item.CreateItem;
using Mapster;

namespace Application.Mapper;

public class ItemMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.ForType<CreateItemCommand, Domain.Items.Item>()
            .Map(dest => dest.ItemId, src => MapContext.Current!.Parameters[nameof(Domain.Items.Item.ItemId)])
            .Map(dest => dest.Name, src => src.ItemName)
            .Map(dest => dest.Description, src => src.ItemDescription);
    }
}
