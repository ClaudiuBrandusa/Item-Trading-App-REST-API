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
using Application.Repositories;
using Application.Results.Items;
using Domain.DomainEvents.Items;
using Application.Behaviors.Trade.ItemUsedInTrade;

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

    public async Task<Result<FullItemResult>> CreateItemAsync(CreateItemCommand model)
    {
        if (model is null || string.IsNullOrEmpty(model.SenderUserId) || string.IsNullOrEmpty(model.ItemName))
            return Result<FullItemResult>.Failure("Something went wrong");

        var item = _mapper.AdaptToType<CreateItemCommand, Domain.Entities.Items.Item>(model, (nameof(Domain.Entities.Items.Item.ItemId), Domain.Entities.Items.Item.GenerateId()));

        if (!await _repository.AddEntityAsync(item))
            return Result<FullItemResult>.Failure("Unable to add this item");

        await _publisher.Publish(new ItemCreatedDomainEvent(item, model.SenderUserId));
        
        return Result<FullItemResult>.Success(new FullItemResult
        {
            ItemId = item.ItemId,
            ItemName = item.Name,
            ItemDescription = item.Description
        });
    }

    public async Task<Result<FullItemResult>> UpdateItemAsync(UpdateItemCommand model)
    {
        if (model is null || string.IsNullOrEmpty(model.ItemName))
            return Result<FullItemResult>.Failure("Something went wrong");

        var item = await _repository.GetItemEntityAsync(model.ItemId, false);

        if (item is null)
            return Result<FullItemResult>.Failure("Something went wrong");

        item.UpdateItemName(model.ItemName);
        item.UpdateItemDescription(model.ItemDescription);

        if (!await _repository.UpdateEntityAsync(item))
            return Result<FullItemResult>.Failure("Unable to update item");

        await _publisher.Publish(new ItemUpdatedDomainEvent(item, model.SenderUserId));
        
        return Result<FullItemResult>.Success(new FullItemResult
        {
            ItemId = item.ItemId,
            ItemName = item.Name,
            ItemDescription = item.Description
        });
    }

    public async Task<Result<DeleteItemResult>> DeleteItemAsync(DeleteItemCommand model)
    {
        if (string.IsNullOrEmpty(model.ItemId))
            return Result<DeleteItemResult>.Failure("Something went wrong");

        var isUsedInATrade = await _sender.Send(new ItemUsedInTradeQuery { ItemId = model.ItemId });

        if (isUsedInATrade)
            return Result<DeleteItemResult>.Failure("Unable to delete an item that is used in a trade");

        var item = await _repository.GetItemEntityAsync(model.ItemId, false);

        if (item is null)
            return Result<DeleteItemResult>.Failure("Something went wrong");

        if (!await _repository.RemoveEntityAsync(item))
            return Result<DeleteItemResult>.Failure("Unable to remove item");

        await _publisher.Publish(new ItemDeletedDomainEvent(model.ItemId, model.UserId));

        return Result<DeleteItemResult>.Success(new DeleteItemResult
        {
            ItemId = model.ItemId,
            ItemName = item.Name
        });
    }

    public async Task<Result<FullItemResult>> GetItemAsync(GetItemQuery model)
    {
        if (string.IsNullOrEmpty(model.ItemId))
            return Result<FullItemResult>.Failure("Something went wrong");

        var item = await _repository.GetItemEntityAsync(model.ItemId);

        if (item is null)
            return Result<FullItemResult>.Failure("Item not found");

        return Result<FullItemResult>.Success(new FullItemResult
        {
            ItemId = item.ItemId,
            ItemName = item.Name,
            ItemDescription = item.Description
        });
    }

    public async Task<Result<ItemsResult>> ListItemsAsync(ListItemsQuery model)
    {
        var items = await _repository.ListItemsAsync();

        if (!string.IsNullOrEmpty(model.SearchString))
            items = items.Where(x => x.Name
                                      .ToLower()
                                      .StartsWith(model.SearchString
                                                       .ToLower()))
                .ToArray();

        return Result<ItemsResult>.Success(new ItemsResult
        {
            ItemsId = items.Select(i => i.ItemId)
        });
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
