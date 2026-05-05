using Application.Behaviors.Inventories.AddItem;
using Application.Behaviors.Inventories.DropItem;
using Application.Behaviors.Inventories.GetItem;
using Application.Behaviors.Inventories.GetLockedAmount;
using Application.Behaviors.Inventories.ListItems;
using Application.Models.Common;
using Application.Results.Inventories;
using Item_Trading_App_Contracts.Requests.Inventory;
using Item_Trading_App_Contracts.Responses.Inventory;
using Mapster;

namespace Item_Trading_App_REST_API.MappingConfigs;

public class InventoryMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.ForType<AddItemRequest, AddInventoryItemCommand>()
            .Map(dest => dest.UserId, src => MapContext.Current!.Parameters[nameof(AddInventoryItemCommand.UserId)])
            .Map(dest => dest.Notify, src => MapContext.Current!.Parameters[nameof(AddInventoryItemCommand.Notify)]);

        config.ForType<Result<QuantifiedItemResult>, AddItemFailedResponse>()
            .IgnoreNonMapped(true)
            .Map(dest => dest.Errors, src => new string[] { src.Error });

        config.ForType<DropItemRequest, DropInventoryItemCommand>()
            .Map(dest => dest.UserId, src => MapContext.Current!.Parameters[nameof(DropInventoryItemCommand.UserId)])
            .Map(dest => dest.Notify, src => MapContext.Current!.Parameters[nameof(DropInventoryItemCommand.Notify)])
            .Map(dest => dest.Quantity, src => src.ItemQuantity);
            
        config.ForType<string, GetInventoryItemQuery>()
            .MapWith(str =>
                new GetInventoryItemQuery { ItemId = str, UserId = MapContext.Current!.Parameters[nameof(GetInventoryItemQuery.UserId)].ToString() ?? string.Empty });

        config.ForType<string, ListInventoryItemsQuery>()
            .MapWith(str => new ListInventoryItemsQuery { SearchString = str, UserId = MapContext.Current!.Parameters[nameof(ListInventoryItemsQuery.UserId)].ToString() ?? string.Empty });

        config.ForType<string, GetInventoryItemLockedAmountQuery>()
            .MapWith(str =>
                new GetInventoryItemLockedAmountQuery { ItemId = str, UserId = MapContext.Current!.Parameters[nameof(GetInventoryItemLockedAmountQuery.UserId)].ToString() ?? string.Empty });
    
        config.ForType<LockedItemAmountResult, GetLockedAmountSuccessResponse>()
            .Map(dest => dest.LockedAmount, src => src.Amount);

        config.ForType<string, AddItemFailedResponse>()
                .MapWith(str =>
                    new AddItemFailedResponse { Errors = new string[] { str } });

        config.ForType<string, DropItemFailedResponse>()
                .MapWith(str =>
                    new DropItemFailedResponse { Errors = new string[] { str } });

        config.ForType<string, GetItemFailedResponse>()
                .MapWith(str =>
                    new GetItemFailedResponse { Errors = new string[] { str } });

        config.ForType<string, GetLockedAmountFailedResponse>()
                .MapWith(str =>
                    new GetLockedAmountFailedResponse { Errors = new string[] { str } });
    }
}

