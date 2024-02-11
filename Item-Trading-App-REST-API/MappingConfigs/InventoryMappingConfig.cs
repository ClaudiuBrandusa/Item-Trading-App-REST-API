using Item_Trading_App_Contracts.Requests.Inventory;
using Item_Trading_App_Contracts.Responses.Inventory;
using Item_Trading_App_REST_API.Models.Inventory;
using Item_Trading_App_REST_API.Models.Item;
using Item_Trading_App_REST_API.Models.TradeItems;
using Item_Trading_App_REST_API.Resources.Commands.Inventory;
using Item_Trading_App_REST_API.Resources.Events.Inventory;
using Item_Trading_App_REST_API.Resources.Queries.Inventory;
using Mapster;

namespace Item_Trading_App_REST_API.MappingConfigs;

public class InventoryMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.ForType<AddItemRequest, AddInventoryItemCommand>()
            .Map(dest => dest.UserId, src => MapContext.Current!.Parameters[nameof(AddInventoryItemCommand.UserId)])
            .Map(dest => dest.Notify, src => MapContext.Current!.Parameters[nameof(AddInventoryItemCommand.Notify)]);

        config.ForType<TradeItem, AddInventoryItemCommand>()
            .Map(dest => dest.UserId, src => MapContext.Current!.Parameters[nameof(AddInventoryItemCommand.UserId)])
            .Map(dest => dest.Notify, src => MapContext.Current!.Parameters[nameof(AddInventoryItemCommand.Notify)]);

        config.ForType<TradeItem, DropInventoryItemCommand>()
            .Map(dest => dest.UserId, src => MapContext.Current!.Parameters[nameof(DropInventoryItemCommand.UserId)])
            .Map(dest => dest.Notify, src => MapContext.Current!.Parameters[nameof(DropInventoryItemCommand.Notify)]);

        config.ForType<QuantifiedItemResult, AddItemFailedResponse>()
            .IgnoreNonMapped(true)
            .Map(dest => dest.Errors, src => src.Errors);

        config.ForType<DropItemRequest, DropInventoryItemCommand>()
            .Map(dest => dest.UserId, src => MapContext.Current!.Parameters[nameof(DropInventoryItemCommand.UserId)])
            .Map(dest => dest.Notify, src => MapContext.Current!.Parameters[nameof(DropInventoryItemCommand.Notify)])
            .Map(dest => dest.Quantity, src => src.ItemQuantity);

        config.ForType<string, GetInventoryItemQuery>()
            .MapWith(str =>
                new GetInventoryItemQuery { ItemId = str, UserId = MapContext.Current!.Parameters[nameof(GetInventoryItemQuery.UserId)].ToString() });

        config.ForType<string, GetInventoryItemLockedAmountQuery>()
            .MapWith(str =>
                new GetInventoryItemLockedAmountQuery { ItemId = str, UserId = MapContext.Current!.Parameters[nameof(GetInventoryItemLockedAmountQuery.UserId)].ToString() });

        config.ForType<string, ListInventoryItemsQuery>()
            .MapWith(str => new ListInventoryItemsQuery { SearchString = str, UserId = MapContext.Current!.Parameters[nameof(ListInventoryItemsQuery.UserId)].ToString() });

        config.ForType<Entities.OwnedItem, InventoryItem>()
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

