using Application.Behaviors.Item.CreateItem;
using Application.Behaviors.Item.UpdateItem;
using Application.Models.Items;
using Domain.Entities.Items;
using Mapster;

namespace Application.Mapper;

public class ItemMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.ForType<CreateItemCommand, Item>()
            .MapWith((CreateItemCommand command) => new Item(
                command.ItemName,
                command.ItemDescription
            ));

        config.ForType<UpdateItemCommand, Item>()
            .MapWith((UpdateItemCommand command) => new Item(
                MapContext.Current!.Parameters[nameof(Item.ItemId)].ToString()!,
                command.ItemName,
                command.ItemDescription
            ));

        config.ForType<CachedItem, Item>()
            .MapWith((CachedItem cachedEntity) => new Item(
                cachedEntity.ItemId,
                cachedEntity.Name,
                cachedEntity.Description
            ));

        config.ForType<Item, CachedItem>()
            .Map(dest => dest.ItemId, src => src.ItemId)
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.Description, src => src.Description);
    }
}
