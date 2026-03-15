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
using Application.Results.Inventories;
using Domain.Entities.Inventories;
using Domain.Entities.Trades;
using Item_Trading_App_Contracts.Responses.Inventory;
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

        config.ForType<InventoryItem, CachedOwnedItem>()
            .MapWith((InventoryItem ownedItem) => new CachedOwnedItem
            {
                ItemId = ownedItem.ItemId,
                UserId = MapContext.Current!.Parameters[nameof(CachedOwnedItem.UserId)]!.ToString(),
                Quantity = ownedItem.Quantity
            });

        config.ForType<CachedOwnedItem, InventoryItem>()
            .MapWith((CachedOwnedItem cachedOwnedItem) => new InventoryItem(
                cachedOwnedItem.UserId,
                cachedOwnedItem.ItemId,
                cachedOwnedItem.Quantity,
                0
            ));

        config.ForType<TradeItemDTO, LockItemCommand>()
            .Map(dest => dest.UserId, src => MapContext.Current!.Parameters[nameof(LockItemCommand.UserId)])
            .Map(dest => dest.Notify, src => MapContext.Current!.Parameters[nameof(LockItemCommand.Notify)]);

        config.ForType<LockedItemAmountResult, GetLockedAmountSuccessResponse>()
            .Map(dest => dest.LockedAmount, src => src.Amount);

        config.ForType<TradeItemDTO, HasItemQuantityQuery>()
            .Map(dest => dest.UserId, src => MapContext.Current!.Parameters[nameof(LockItemCommand.UserId)])
            .Map(dest => dest.Notify, src => MapContext.Current!.Parameters[nameof(LockItemCommand.Notify)]);

        config.ForType<TradeItem, UnlockItemCommand>()
            .Map(dest => dest.UserId, src => MapContext.Current!.Parameters[nameof(UnlockItemCommand.UserId)])
            .Map(dest => dest.Notify, src => MapContext.Current!.Parameters[nameof(UnlockItemCommand.Notify)]);
    }
}

