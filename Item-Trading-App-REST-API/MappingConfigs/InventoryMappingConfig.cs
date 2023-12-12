using Item_Trading_App_Contracts.Requests.Inventory;
using Item_Trading_App_Contracts.Responses.Inventory;
using Item_Trading_App_REST_API.Models.Inventory;
using Item_Trading_App_REST_API.Models.Item;
using Mapster;

namespace Item_Trading_App_REST_API.MappingConfigs;

public class InventoryMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.ForType<AddItemRequest, AddItem>()
            .Map(dest => dest.UserId, src => MapContext.Current!.Parameters["userId"]);

        config.ForType<QuantifiedItemResult, AddItemFailedResponse>()
            .IgnoreNonMapped(true)
            .Map(dest => dest.Errors, src => src.Errors);

        config.ForType<DropItemRequest, DropItem>()
            .Map(dest => dest.UserId, src => MapContext.Current!.Parameters["userId"])
            .Map(dest => dest.Quantity, src => src.ItemQuantity);

        config.ForType<string, GetUsersItem>()
            .MapWith(str =>
                new GetUsersItem { ItemId = str, UserId = MapContext.Current!.Parameters["userId"].ToString() });

        config.ForType<string, ListItems>()
            .MapWith(str => new ListItems { SearchString = str, UserId = MapContext.Current!.Parameters["userId"].ToString() });
    }
}
