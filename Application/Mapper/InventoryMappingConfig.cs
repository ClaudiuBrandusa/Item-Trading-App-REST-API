using Application.Behaviors.Inventories.AddItem;
using Application.Behaviors.Inventories.DropItem;
using Application.Behaviors.Inventories.GetItem;
using Application.Behaviors.Inventories.GetLockedAmount;
using Application.Behaviors.Inventories.HasItem;
using Application.Behaviors.Inventories.ListItems;
using Application.Behaviors.Inventories.LockItem;
using Application.Behaviors.Inventories.UnlockItem;
using Application.Models.Inventories;
using Application.Models.TradeItems;
using Domain.Aggregates.Inventories;
using Domain.Entities.Trades;
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
                new GetInventoryItemQuery { ItemId = str, UserId = MapContext.Current!.Parameters[nameof(GetInventoryItemQuery.UserId)].ToString() ?? string.Empty });

        config.ForType<string, GetInventoryItemLockedAmountQuery>()
            .MapWith(str =>
                new GetInventoryItemLockedAmountQuery { ItemId = str, UserId = MapContext.Current!.Parameters[nameof(GetInventoryItemLockedAmountQuery.UserId)].ToString() ?? string.Empty });

        config.ForType<string, ListInventoryItemsQuery>()
            .MapWith(str => new ListInventoryItemsQuery { SearchString = str, UserId = MapContext.Current!.Parameters[nameof(ListInventoryItemsQuery.UserId)].ToString() ?? string.Empty });

        config.ForType<AddInventoryItemCommand, OwnedItem>()
            .MapWith((AddInventoryItemCommand command) => new OwnedItem(
                command.ItemId,
                command.UserId,
                command.Quantity
            ));

        config.ForType<DropInventoryItemCommand, OwnedItem>()
            .MapWith((DropInventoryItemCommand command) => new OwnedItem(
                command.ItemId,
                command.UserId,
                command.Quantity
            ));

        config.ForType<OwnedItem, CachedOwnedItem>()
            .MapWith((OwnedItem ownedItem) => new CachedOwnedItem
            {
                ItemId = ownedItem.ItemId,
                UserId = ownedItem.UserId,
                Quantity = ownedItem.Quantity
            });

        config.ForType<CachedOwnedItem, OwnedItem>()
            .MapWith((CachedOwnedItem cachedOwnedItem) => new OwnedItem(
                cachedOwnedItem.ItemId,
                cachedOwnedItem.UserId,
                cachedOwnedItem.Quantity
            ));

        config.ForType<TradeItemDTO, LockItemCommand>()
            .Map(dest => dest.UserId, src => MapContext.Current!.Parameters[nameof(LockItemCommand.UserId)])
            .Map(dest => dest.Notify, src => MapContext.Current!.Parameters[nameof(LockItemCommand.Notify)]);

        config.ForType<TradeItemDTO, HasItemQuantityQuery>()
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

