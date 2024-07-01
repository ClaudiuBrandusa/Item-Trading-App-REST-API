using Application.Behaviors.Inventory.AddItem;
using Application.Behaviors.Inventory.DropItem;
using Application.Behaviors.Inventory.GetItem;
using Application.Behaviors.Inventory.GetLockedAmount;
using Application.Behaviors.Inventory.HasItem;
using Application.Behaviors.Inventory.ListItems;
using Application.Behaviors.Inventory.LockItem;
using Application.Behaviors.Inventory.UnlockItem;
using Domain.Inventory;
using Domain.TradeItems;
using Mapster;

namespace Application.Mapper;

public class InventoryMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.ForType<TradeItem, AddInventoryItemCommand>()
            .Map(dest => dest.UserId, src => MapContext.Current!.Parameters[nameof(AddInventoryItemCommand.UserId)])
            .Map(dest => dest.Notify, src => MapContext.Current!.Parameters[nameof(AddInventoryItemCommand.Notify)]);

        config.ForType<TradeItem, DropInventoryItemCommand>()
            .Map(dest => dest.UserId, src => MapContext.Current!.Parameters[nameof(DropInventoryItemCommand.UserId)])
            .Map(dest => dest.Notify, src => MapContext.Current!.Parameters[nameof(DropInventoryItemCommand.Notify)]);

        config.ForType<string, GetInventoryItemQuery>()
            .MapWith(str =>
                new GetInventoryItemQuery { ItemId = str, UserId = MapContext.Current!.Parameters[nameof(GetInventoryItemQuery.UserId)].ToString() });

        config.ForType<string, GetInventoryItemLockedAmountQuery>()
            .MapWith(str =>
                new GetInventoryItemLockedAmountQuery { ItemId = str, UserId = MapContext.Current!.Parameters[nameof(GetInventoryItemLockedAmountQuery.UserId)].ToString() });

        config.ForType<string, ListInventoryItemsQuery>()
            .MapWith(str => new ListInventoryItemsQuery { SearchString = str, UserId = MapContext.Current!.Parameters[nameof(ListInventoryItemsQuery.UserId)].ToString() });

        config.ForType<OwnedItem, InventoryItem>()
            .Map(dest => dest.Id, src => src.ItemId);

        config.ForType<AddInventoryItemCommand, InventoryItem>()
            .Map(dest => dest.Id, src => src.ItemId);
        
        config.ForType<TradeItem, LockItemCommand>()
            .Map(dest => dest.UserId, src => MapContext.Current!.Parameters[nameof(LockItemCommand.UserId)])
            .Map(dest => dest.Notify, src => MapContext.Current!.Parameters[nameof(LockItemCommand.Notify)]);

        config.ForType<TradeItem, HasItemQuantityQuery>()
            .Map(dest => dest.UserId, src => MapContext.Current!.Parameters[nameof(LockItemCommand.UserId)])
            .Map(dest => dest.Notify, src => MapContext.Current!.Parameters[nameof(LockItemCommand.Notify)]);

        config.ForType<TradeItem, UnlockItemCommand>()
            .Map(dest => dest.UserId, src => MapContext.Current!.Parameters[nameof(UnlockItemCommand.UserId)])
            .Map(dest => dest.Notify, src => MapContext.Current!.Parameters[nameof(UnlockItemCommand.Notify)]);

        config.ForType<LockItemCommand, InventoryItemLockedEvent>()
            .Map(dest => dest.Quantity, src => MapContext.Current!.Parameters[nameof(InventoryItemLockedEvent.Quantity)]);

        config.ForType<UnlockItemCommand, InventoryItemUnlockedEvent>()
            .Map(dest => dest.Quantity, src => MapContext.Current!.Parameters[nameof(InventoryItemUnlockedEvent.Quantity)]);
    }
}

