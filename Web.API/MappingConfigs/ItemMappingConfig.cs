using Application.Behaviors.Item.CreateItem;
using Application.Behaviors.Item.DeleteItem;
using Application.Behaviors.Item.UpdateItem;
using Application.Results.Items;
using Item_Trading_App_Contracts.Requests.Item;
using Item_Trading_App_Contracts.Responses.Item;
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

        config.ForType<CreateItemRequest, CreateItemCommand>()
            .Map(dest => dest.SenderUserId, src => MapContext.Current!.Parameters[nameof(CreateItemCommand.SenderUserId)]);

        config.ForType<UpdateItemRequest, UpdateItemCommand>()
            .Map(dest => dest.SenderUserId, src => MapContext.Current!.Parameters[nameof(UpdateItemCommand.SenderUserId)]);

        config.ForType<DeleteItemRequest, DeleteItemCommand>()
            .Map(dest => dest.UserId, src => MapContext.Current!.Parameters[nameof(DeleteItemCommand.UserId)]);
    }
}
