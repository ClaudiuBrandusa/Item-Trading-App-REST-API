using MapsterMapper;
using MediatR;
using Application.Constants;
using Application.Extensions;
using Application.Behaviors.Item.GetItemName;
using Application.Behaviors.Item.GetItem;
using Application.Services.Notification;
using Application.Repositories;
using Application.Results.Items;
using Application.Behaviors.Inventories.HasItem;
using Application.Results.Inventories;
using Application.Behaviors.Inventories.AddItem;
using Application.Behaviors.Inventories.DropItem;
using Application.Behaviors.Inventories.GetItem;
using Application.Behaviors.Inventories.GetLockedAmount;
using Application.Behaviors.Inventories.ListItems;
using Application.Behaviors.Inventories.LockItem;
using Application.Behaviors.Inventories.UnlockItem;
using Application.Behaviors.Inventories.ListUsersOwningItem;
using Application.Behaviors.Inventories.RemoveItemFromUsers;
using Application.Models.Inventories;
using Domain.Entities.Inventories;
using Application.Helpers;
using Domain.Aggregates.Inventories;

namespace Application.Services.Inventories;

public class InventoryService : IInventoryService, IDisposable
{
    private readonly ICachedInventoryRepository _repository;
    private readonly IClientNotificationService _clientNotificationService;
    private readonly ISender _sender;
    private readonly IPublisher _publisher;
    private readonly IMapper _mapper;

    public InventoryService(ICachedInventoryRepository inventoryRepository, IClientNotificationService clientNotificationService, ISender sender, IPublisher publisher, IMapper mapper)
    {
        _repository = inventoryRepository;
        _clientNotificationService = clientNotificationService;
        _sender = sender;
        _publisher = publisher;
        _mapper = mapper;
    }

    public async Task<bool> HasItemAsync(HasItemQuantityQuery model)
    {
        if (string.IsNullOrEmpty(model.UserId) || string.IsNullOrEmpty(model.ItemId) || model.Quantity < 1)
            return false;

        var amount = await _repository.GetAmountOfFreeItemAsync(model.UserId, model.ItemId);

        return amount >= model.Quantity;
    }

    public async Task<QuantifiedItemResult> AddItemAsync(AddInventoryItemCommand model)
    {
        if (string.IsNullOrEmpty(model.UserId) || string.IsNullOrEmpty(model.ItemId))
            return new QuantifiedItemResult
            {
                Errors = new[] { "Something went wrong" }
            };

        if (model.Quantity < 0)
            return new QuantifiedItemResult
            {
                Errors = new[] { "You cannot add a negative amount of an item" }
            };
        else if (model.Quantity == 0)
            return new QuantifiedItemResult
            {
                Errors = new[] { "You cannot add an amount of 0 in your inventory" }
            };

        var itemData = await _sender.Send(new GetItemQuery { ItemId = model.ItemId });

        if (itemData is null)
            return new QuantifiedItemResult
            {
                Errors = new[] { "Item not found" }
            };

        bool modified;

        var inventory = await _repository.GetInventoryAsync(model.UserId);

        if (inventory is null)
        {
            inventory = new Inventory(model.UserId);
        }

        inventory.AddItem(model.ItemId, model.Quantity);

        modified = await _repository.AddInventoryAsync(inventory);

        if (!modified)
            return new QuantifiedItemResult
            {
                Errors = new[] { "Something went wrong" }
            };

        var eventNotification = _mapper.AdaptToType<AddInventoryItemCommand, InventoryItemAddedEvent>(model);

        await _publisher.Publish(eventNotification);

        return new QuantifiedItemResult
        {
            ItemId = model.ItemId,
            ItemName = itemData.ItemName,
            ItemDescription = itemData.ItemDescription,
            Quantity = inventory.GetItemFreeAmount(model.ItemId),
            Success = true
        };
    }

    public async Task<QuantifiedItemResult> DropItemAsync(DropInventoryItemCommand model)
    {
        if (string.IsNullOrEmpty(model.UserId) || string.IsNullOrEmpty(model.ItemId))
            return new QuantifiedItemResult
            {
                Errors = new[] { "Something went wrong" }
            };

        if (model.Quantity < 0)
            return new QuantifiedItemResult
            {
                Errors = new[] { "You cannot drop a negative amount of an item" }
            };
        else if (model.Quantity == 0)
            return new QuantifiedItemResult
            {
                Errors = new[] { "You cannot drop an amount of 0 from your inventory" }
            };

        var inventory = await _repository.GetInventoryAsync(model.UserId);

        if (!inventory.ItemIds.Contains(model.ItemId))
            return new QuantifiedItemResult
            {
                Errors = new[] { "Item is not part of the inventory" }
            };

        try
        {
            inventory.DropItem(model.ItemId, model.Quantity);
        }
        catch (ArgumentException ex)
        {
            return new QuantifiedItemResult
            {
                Errors = new[] { ex.Message }
            };
        }

        bool modified = await _repository.DropItemAsync(model.UserId, model.ItemId, model.Quantity);

        if (!modified)
            return new QuantifiedItemResult
            {
                Errors = new[] { "Something went wrong" }
            };

        var eventNotification = _mapper.AdaptToType<DropInventoryItemCommand, InventoryItemDroppedEvent>(model);

        await _publisher.Publish(eventNotification);

        return new QuantifiedItemResult
        {
            ItemId = model.ItemId,
            ItemName = await _sender.Send(new GetItemNameQuery { ItemId = model.ItemId }),
            Quantity = inventory.GetItemFreeAmount(model.ItemId),
            Success = true
        };
    }

    public async Task<QuantifiedItemResult> GetItemAsync(GetInventoryItemQuery model)
    {
        if (string.IsNullOrEmpty(model.UserId) || string.IsNullOrEmpty(model.ItemId))
            return new QuantifiedItemResult
            {
                Errors = new[] { "Something went wrong" }
            };

        int amount = await _repository.GetAmountOfFreeItemAsync(model.UserId, model.ItemId);
        var lockedAmount = await GetLockedAmountAsync(_mapper.AdaptToType<GetInventoryItemQuery, GetInventoryItemLockedAmountQuery>(model));

        if (amount == 0 && lockedAmount.Amount == 0)
            return new QuantifiedItemResult
            {
                Errors = new[] { "You do not own this item" }
            };

        var itemData = await _sender.Send(new GetItemQuery { ItemId = model.ItemId });

        return new QuantifiedItemResult
        {
            ItemId = model.ItemId,
            ItemName = itemData.ItemName,
            ItemDescription = itemData.ItemDescription,
            Quantity = amount,
            Success = true
        };
    }

    public async Task<ItemsResult> ListItemsAsync(ListInventoryItemsQuery model)
    {
        if (string.IsNullOrEmpty(model.UserId))
            return new ItemsResult
            {
                Errors = new[] { "Something went wrong" }
            };

        var itemIds = await FilterInventoryItems(model.UserId, model.SearchString);

        return new ItemsResult
        {
            Success = true,
            ItemsId = itemIds
        };
    }

    public async Task<LockItemResult> LockItemAsync(LockItemCommand model)
    {
        if (string.IsNullOrEmpty(model.UserId) || string.IsNullOrEmpty(model.ItemId) || model.Quantity < 1)
            return new LockItemResult
            {
                Errors = new[] { "Invalid input data" }
            };

        int amount = await _repository.GetAmountOfFreeItemAsync(model.UserId, model.ItemId);

        if (amount < model.Quantity)
            return new LockItemResult
            {
                Errors = new[] { "You do not own enough of this item" }
            };

        if (!await _repository.LockItemAsync(model.UserId, model.ItemId, model.Quantity))
            return new LockItemResult
            {
                Errors = new[] { "Something went wrong" }
            };
        else
            amount -= model.Quantity;

        var notificationEvent = _mapper.AdaptToType<LockItemCommand, InventoryItemLockedEvent>(model, (nameof(InventoryItemLockedEvent.Quantity), amount));

        await _publisher.Publish(notificationEvent);

        return new LockItemResult
        {
            UserId = model.UserId,
            ItemId = model.ItemId,
            Quantity = amount,
            Success = true
        };
    }

    public async Task<LockItemResult> UnlockItemAsync(UnlockItemCommand model)
    {
        if (string.IsNullOrEmpty(model.UserId) || string.IsNullOrEmpty(model.ItemId) || model.Quantity < 1)
            return new LockItemResult
            {
                Errors = new[] { "Invalid input data" }
            };

        int amount = await _repository.GetAmountOfLockedItemAsync(model.UserId, model.ItemId);

        if (amount == 0 || model.Quantity > amount)
            return new LockItemResult
            {
                Errors = new[] { "Cannot unlock more than you have locked" }
            };

        amount -= model.Quantity;
        bool modified = false;
        
        try
        {
            modified = await _repository.UnlockItemAsync(model.UserId, model.ItemId, model.Quantity);
        }
        catch (Exception)
        {
            modified = false;
        }

        if (!modified)
            return new LockItemResult
            {
                Errors = new[] { "Something went wrong" }
            };

        amount = await _repository.GetAmountOfFreeItemAsync(model.UserId, model.ItemId);

        var eventNotification = _mapper.AdaptToType<UnlockItemCommand, InventoryItemUnlockedEvent>(model, (nameof(InventoryItemUnlockedEvent.Quantity), await _repository.GetAmountOfFreeItemAsync(model.UserId, model.ItemId)));

        await _publisher.Publish(eventNotification);

        return new LockItemResult
        {
            ItemId = model.ItemId,
            UserId = model.UserId,
            Quantity = amount,
            Success = true
        };
    }

    public async Task<LockedItemAmountResult> GetLockedAmountAsync(GetInventoryItemLockedAmountQuery model)
    {
        if (string.IsNullOrEmpty(model.UserId) || string.IsNullOrEmpty(model.ItemId))
            return new LockedItemAmountResult
            {
                Errors = new[] { "Invalid input data" }
            };

        int lockedAmount = await _repository.GetAmountOfLockedItemAsync(model.UserId, model.ItemId);

        var itemName = await _sender.Send(new GetItemNameQuery { ItemId = model.ItemId });

        if (string.IsNullOrEmpty(itemName))
            return new LockedItemAmountResult
            {
                Errors = new[] { "Item not found" }
            };

        return new LockedItemAmountResult
        {
            ItemId = model.ItemId,
            ItemName = itemName,
            Amount = lockedAmount,
            Success = true
        };
    }

    public async Task<UsersOwningItem> GetUsersOwningThisItemAsync(GetUserIdsOwningItemQuery model) =>
        new UsersOwningItem
        {
            UserIds = await _repository.ListUsersThatOwnItemAsync(model.ItemId),
            ItemId = model.ItemId
        };

    public async Task RemoveItemCacheAsync(RemoveItemFromUsersCommand model)
    {
        var tasks = new Task[model.UserIds.Length];

        for (int i = 0; i < model.UserIds.Length; i++)
        {
            string userId = model.UserIds[i];

            tasks[i] = _repository.RemoveItemCacheForUserAsync(userId, model.ItemId);
        };

        await Task.WhenAll(tasks);

        if (model.UserIds.Length == 0)
            return;

        var notificationStrategy = NotificationHelper.CreateMultipleUsersNotificationStrategy(model.UserIds);

        await _clientNotificationService.SendDeletedNotificationAsync(notificationStrategy, NotificationCategoryTypes.Inventory, model.ItemId);
    }

    public void Dispose()
    {
        _repository.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task<List<string>> FilterInventoryItems(string userId, string searchString)
    {
        var inventory = await _repository.GetInventoryAsync(userId);

        if (inventory is null)
            return new();

        var inventoryItems = inventory.OwnedItems;

        List<string> itemIds = new();

        if (!string.IsNullOrEmpty(searchString))
        {
            foreach (var inventoryItem in inventoryItems)
            {
                await FilterInventoryItemBySearchString(inventoryItem, searchString, itemIds);
            };
        }
        else
        {
            itemIds = inventoryItems.Select(x => x.ItemId).ToList();
        }

        return itemIds;
    }

    private async Task FilterInventoryItemBySearchString(InventoryItem inventoryItem, string searchString, List<string> itemIds)
    {
        string itemName = await _sender.Send(new GetItemNameQuery { ItemId = inventoryItem.ItemId });

        if (!itemName.StartsWith(searchString, StringComparison.OrdinalIgnoreCase)) return;

        itemIds.Add(inventoryItem.ItemId);
    }
}
