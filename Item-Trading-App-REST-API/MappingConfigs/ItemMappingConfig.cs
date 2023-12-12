using Item_Trading_App_Contracts.Requests.Item;
using Item_Trading_App_Contracts.Responses.Item;
using Item_Trading_App_REST_API.Models.Item;
using Mapster;

namespace Item_Trading_App_REST_API.MappingConfigs;

public class ItemMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.ForType<FullItemResult, ItemResponse>()
            .Map(dest => dest.Id, src => src.ItemId)
            .Map(dest => dest.Name, src => src.ItemName)
            .Map(dest => dest.Description, src => src.ItemDescription);

        config.ForType<CreateItemRequest, CreateItem>()
            .Map(dest => dest.SenderUserId, src => MapContext.Current!.Parameters["userId"]);

        config.ForType<UpdateItemRequest, UpdateItem>()
            .Map(dest => dest.SenderUserId, src => MapContext.Current!.Parameters["userId"]);
    }
}
