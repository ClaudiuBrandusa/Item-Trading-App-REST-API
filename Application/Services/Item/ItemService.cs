using MediatR;
using MapsterMapper;
using Application.Extensions;
using Application.Behaviors.Item.CreateItem;
using Application.Behaviors.Item.UpdateItem;
using Application.Behaviors.Item.GetItem;
using Application.Behaviors.Item.DeleteItem;
using Application.Behaviors.Item.ListItems;
using Application.Behaviors.Item.GetItemName;
using Application.Behaviors.Item.GetItemDescription;
using Application.Behaviors.TradeItem.ItemUsedInTrade;
using Domain.Repositories.Items;
using Application.Results.Items;

namespace Application.Services.Item;

public class ItemService : IItemService, IDisposable
{
    private readonly ICachedItemRepository _repository;
    private readonly ISender _sender;
    private readonly IPublisher _publisher;
    private readonly IMapper _mapper;

    public ItemService(ICachedItemRepository repository, ISender sender, IPublisher publisher, IMapper mapper)
    {
        _repository = repository;
        _sender = sender;
        _publisher = publisher;
        _mapper = mapper;
    }

    public async Task<FullItemResult> CreateItemAsync(CreateItemCommand model)
    {
        if (model is null || string.IsNullOrEmpty(model.SenderUserId) || string.IsNullOrEmpty(model.ItemName))
            return new FullItemResult
            {
                Errors = new[] { "Something went wrong" }
            };

        var item = _mapper.AdaptToType<CreateItemCommand, Domain.Entities.Items.Item>(model, (nameof(Domain.Entities.Items.Item.ItemId), Domain.Entities.Items.Item.GenerateId()));

        if (!await _repository.AddEntityAsync(item))
            return new FullItemResult
            {
                Errors = new[] { "Unable to add this item" }
            };

        await _publisher.Publish(new ItemCreatedEvent { Item = item, SenderUserId = model.SenderUserId });
        
        return new FullItemResult
        {
            ItemId = item.ItemId,
            ItemName = item.Name,
            ItemDescription = item.Description,
            Success = true
        };
    }

    public async Task<FullItemResult> UpdateItemAsync(UpdateItemCommand model)
    {
        if (model is null || string.IsNullOrEmpty(model.ItemName))
            return new FullItemResult
            {
                Errors = new[] { "Something went wrong" }
            };

        var item = await _repository.GetItemEntityAsync(model.ItemId, false);

        if (item is null)
            return new FullItemResult
            {
                Errors = new[] { "Something went wrong" }
            };

        item.UpdateItemName(model.ItemName);
        item.UpdateItemDescription(model.ItemDescription);

        if (!await _repository.UpdateEntityAsync(item))
            return new FullItemResult
            {
                ItemId = item.ItemId,
                ItemName = item.Name,
                ItemDescription = item.Description,
                Errors = new[] { "Unable to update item" }
            };

        await _publisher.Publish(new ItemUpdatedEvent { Item = item, SenderUserId = model.SenderUserId });
        
        return new FullItemResult
        {
            ItemId = item.ItemId,
            ItemName = item.Name,
            ItemDescription = item.Description,
            Success = true
        };
    }

    public async Task<DeleteItemResult> DeleteItemAsync(DeleteItemCommand model)
    {
        if (string.IsNullOrEmpty(model.ItemId))
            return new DeleteItemResult
            {
                Errors = new[] { "Something went wrong" }
            };

        var isUsedInATrade = await _sender.Send(new ItemUsedInTradeQuery { ItemId = model.ItemId });

        if (isUsedInATrade)
            return new DeleteItemResult
            {
                Errors = new[] { "Unable to delete an item that is used in a trade" }
            };

        var item = await _repository.GetItemEntityAsync(model.ItemId, false);

        if (item is null)
            return new DeleteItemResult
            {
                Errors = new[] { "Something went wrong" }
            };

        if (!await _repository.RemoveEntityAsync(item))
            return new DeleteItemResult
            {
                Errors = new[] { "Unable to remove item" }
            };

        await _publisher.Publish(new ItemDeletedEvent { ItemId = model.ItemId, UserId = model.UserId });

        return new DeleteItemResult
        {
            ItemId = model.ItemId,
            ItemName = item.Name,
            Success = true
        };
    }

    public async Task<FullItemResult> GetItemAsync(GetItemQuery model)
    {
        if (string.IsNullOrEmpty(model.ItemId))
            return new FullItemResult
            {
                Errors = new[] { "Something went wrong" }
            };

        var item = await _repository.GetItemEntityAsync(model.ItemId);

        if (item is null)
            return new FullItemResult
            {
                ItemId = model.ItemId,
                Errors = new[] { "Item not found" }
            };

        return new FullItemResult
        {
            ItemId = item.ItemId,
            ItemName = item.Name,
            ItemDescription = item.Description,
            Success = true
        };
    }

    public async Task<ItemsResult> ListItemsAsync(ListItemsQuery model)
    {
        var items = await _repository.ListItemsAsync();

        if (!string.IsNullOrEmpty(model.SearchString))
            items = items.Where(x => x.Name
                                      .ToLower()
                                      .StartsWith(model.SearchString
                                                       .ToLower()))
                .ToArray();

        return new ItemsResult
        {
            ItemsId = items.Select(i => i.ItemId),
            Success = true
        };
    }

    public async Task<string> GetItemNameAsync(GetItemNameQuery model)
    {
        var entity = await _repository.GetItemEntityAsync(model.ItemId);

        return entity?.Name ?? string.Empty;
    }

    public async Task<string> GetItemDescriptionAsync(GetItemDescriptionQuery model)
    {
        var entity = await _repository.GetItemEntityAsync(model.ItemId);

        return entity?.Description ?? string.Empty;
    }

    public void Dispose()
    {
        _repository.Dispose();
        GC.SuppressFinalize(this);
    }
}
