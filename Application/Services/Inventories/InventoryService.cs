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
using Application.Behaviors.Inventories.LockItems;
using Application.Models.TradeItems;

namespace Application.Services.Inventories;

public class InventoryService : IInventoryService, IDisposable
{
    private readonly ICachedInventoryRepository _repository;
    private readonly IClientNotificationService _clientNotificationService;
    private readonly ISender _sender;
    private readonly IMapper _mapper;

    public InventoryService(ICachedInventoryRepository inventoryRepository, IClientNotificationService clientNotificationService, ISender sender, IMapper mapper)
    {
        _repository = inventoryRepository;
        _clientNotificationService = clientNotificationService;
        _sender = sender;
        _mapper = mapper;
    }

    public async Task<bool> HasItemAsync(HasItemQuantityQuery model)
    {
        if (string.IsNullOrEmpty(model.UserId) || string.IsNullOrEmpty(model.ItemId) || model.Quantity < 1)
            return false;

        var amount = await _repository.GetAmountOfFreeItemAsync(model.UserId, model.ItemId);

        return amount >= model.Quantity;
    }

    public async Task<Result<QuantifiedItemResult>> AddItemAsync(AddInventoryItemCommand model)
    {
        if (string.IsNullOrEmpty(model.UserId) || string.IsNullOrEmpty(model.ItemId))
            return Result<QuantifiedItemResult>.Failure("Something went wrong");

        if (model.Quantity < 0)
            return Result<QuantifiedItemResult>.Failure("You cannot add a negative amount of an item");
                    
        else if (model.Quantity == 0)
            return Result<QuantifiedItemResult>.Failure("You cannot add an amount of 0 in your inventory");

        var itemDataResult = await _sender.Send(new GetItemQuery { ItemId = model.ItemId });

        if (!itemDataResult.IsSuccess)
            return Result<QuantifiedItemResult>.Failure("Item not found");

        var itemData = itemDataResult.Content!;

        bool modified;

        var inventory = await _repository.LoadInventoryAsync(model.UserId);

        inventory.AddItem(model.ItemId, model.Quantity, model.Notify);

        modified = await _repository.AddInventoryAsync(inventory);

        if (!modified)
            return Result<QuantifiedItemResult>.Failure("Something went wrong");

        return Result<QuantifiedItemResult>.Success(new QuantifiedItemResult
        {
            ItemId = model.ItemId,
            ItemName = itemData.ItemName,
            ItemDescription = itemData.ItemDescription,
            Quantity = inventory.GetItemFreeAmount(model.ItemId)
        });
    }

    public async Task<Result<QuantifiedItemResult>> DropItemAsync(DropInventoryItemCommand model)
    {
        if (string.IsNullOrEmpty(model.UserId) || string.IsNullOrEmpty(model.ItemId))
            return Result<QuantifiedItemResult>.Failure("Something went wrong");

        if (model.Quantity < 0)
            return Result<QuantifiedItemResult>.Failure("You cannot drop a negative amount of an item");
        else if (model.Quantity == 0)
            return Result<QuantifiedItemResult>.Failure("You cannot drop an amount of 0 from your inventory");

        var inventory = await _repository.LoadInventoryAsync(model.UserId);

        if (inventory is null)
            return Result<QuantifiedItemResult>.Failure("Something went wrong");

        if (!inventory.ItemIds.Contains(model.ItemId))
            return Result<QuantifiedItemResult>.Failure("Item is not part of the inventory");
        try
        {
            inventory.DropItem(model.ItemId, model.Quantity);
        }
        catch (ArgumentException ex)
        {
            return Result<QuantifiedItemResult>.Failure(ex.Message);
        }

        bool modified = await _repository.DropItemAsync(inventory, model.ItemId, model.Quantity);

        if (!modified)
            return Result<QuantifiedItemResult>.Failure("Something went wrong");

        return Result<QuantifiedItemResult>.Success(new QuantifiedItemResult
        {
            ItemId = model.ItemId,
            ItemName = await _sender.Send(new GetItemNameQuery { ItemId = model.ItemId }),
            Quantity = await _repository.GetAmountOfFreeItemAsync(model.UserId, model.ItemId)
        });
    }

    public async Task<Result<QuantifiedItemResult>> GetItemAsync(GetInventoryItemQuery model)
    {
        if (string.IsNullOrEmpty(model.UserId) || string.IsNullOrEmpty(model.ItemId))
            return Result<QuantifiedItemResult>.Failure("Something went wrong");

        int amount = await _repository.GetAmountOfFreeItemAsync(model.UserId, model.ItemId);
        var lockedAmountResult = await GetLockedAmountAsync(_mapper.AdaptToType<GetInventoryItemQuery, GetInventoryItemLockedAmountQuery>(model));

        if (!lockedAmountResult.IsSuccess)
            return Result<QuantifiedItemResult>.Failure(lockedAmountResult.Error!);

        if (amount == 0 && lockedAmountResult.Content!.Amount == 0)
            return Result<QuantifiedItemResult>.Failure("You do not own this item");

        var itemDataResult = await _sender.Send(new GetItemQuery { ItemId = model.ItemId });

        if (!itemDataResult.IsSuccess)
            return Result<QuantifiedItemResult>.Failure(itemDataResult.Error!);

        var itemData = itemDataResult.Content!;

        return Result<QuantifiedItemResult>.Success(new QuantifiedItemResult
        {
            ItemId = model.ItemId,
            ItemName = itemData.ItemName,
            ItemDescription = itemData.ItemDescription,
            Quantity = amount
        });
    }

    public async Task<Result<ItemsResult>> ListItemsAsync(ListInventoryItemsQuery model)
    {
        if (string.IsNullOrEmpty(model.UserId))
            return Result<ItemsResult>.Failure("Something went wrong");

        var itemIds = await FilterInventoryItems(model.UserId, model.SearchString);

        return Result<ItemsResult>.Success(new ItemsResult
        {
            ItemsId = itemIds
        });
    }

    public async Task<Result<LockItemResult>> LockItemAsync(LockItemCommand model)
    {
        if (string.IsNullOrEmpty(model.UserId) || string.IsNullOrEmpty(model.ItemId) || model.Quantity < 1)
            return Result<LockItemResult>.Failure("Invalid input data");

        int amount = await _repository.GetAmountOfFreeItemAsync(model.UserId, model.ItemId);

        if (amount < model.Quantity)
            return Result<LockItemResult>.Failure("You do not own enough of this item");

        var inventory = await _repository.LoadInventoryAsync(model.UserId);

        inventory.LockItem(model.ItemId, model.Quantity);

        if (!await _repository.LockItemAsync(inventory, model.ItemId, model.Quantity))
            return Result<LockItemResult>.Failure("Something went wrong");
        else
            amount -= model.Quantity;

        return Result<LockItemResult>.Success(new LockItemResult
        {
            UserId = model.UserId,
            ItemId = model.ItemId,
            Quantity = amount
        });
    }

    public async Task<Result<LockItemsResult>> LockItemsAsync(LockItemsCommand model)
    {
        if (string.IsNullOrEmpty(model.UserId) || model.Items is null || model.Items.Length == 0)
            return Result<LockItemsResult>.Failure("Invalid input data");

        var inventory = await _repository.LoadInventoryAsync(model.UserId);

        if (inventory is null)
            return Result<LockItemsResult>.Failure("Something went wrong");

        var items = new TradeItemDTO[model.Items.Length];

        int index = 0;

        foreach ((var itemId, var quantity) in model.Items)
        {
            try
            {
                inventory.LockItem(itemId, quantity);
                items[index++] = new TradeItemDTO
                {
                    ItemId = itemId,
                    Quantity = inventory.GetItemFreeAmount(itemId)
                };
            }
            catch (ArgumentException ex)
            {
                return Result<LockItemsResult>.Failure(ex.Message);
            }
        }

        foreach (var item in items)
        {
            item.ItemName = await _sender.Send(new GetItemNameQuery { ItemId = item.ItemId });
        }

        if (!await _repository.LockItemsAsync(inventory, model.Items))
            return Result<LockItemsResult>.Failure("Something went wrong");

        return Result<LockItemsResult>.Success(new LockItemsResult(model.UserId, items));
    }

    public async Task<Result<LockItemResult>> UnlockItemAsync(UnlockItemCommand model)
    {
        if (string.IsNullOrEmpty(model.UserId) || string.IsNullOrEmpty(model.ItemId) || model.Quantity < 1)
            return Result<LockItemResult>.Failure("Invalid input data");

        var inventory = await _repository.LoadInventoryAsync(model.UserId);

        try
        {
            inventory.UnlockItem(model.ItemId, model.Quantity);
        }
        catch (ArgumentException ex)
        {
            return Result<LockItemResult>.Failure(ex.Message);
        }

        bool modified = false;

        try
        {
            modified = await _repository.UnlockItemAsync(inventory, model.ItemId, model.Quantity);
        }
        catch (Exception)
        {
            modified = false;
        }

        if (!modified)
            return Result<LockItemResult>.Failure("Something went wrong");

        var freeItemAmount = await _repository.GetAmountOfFreeItemAsync(model.UserId, model.ItemId);

        return Result<LockItemResult>.Success(new LockItemResult
        {
            ItemId = model.ItemId,
            UserId = model.UserId,
            Quantity = freeItemAmount
        });
    }

    public async Task<Result<LockedItemAmountResult>> GetLockedAmountAsync(GetInventoryItemLockedAmountQuery model)
    {
        if (string.IsNullOrEmpty(model.UserId) || string.IsNullOrEmpty(model.ItemId))
            return Result<LockedItemAmountResult>.Failure("Invalid input data");

        int lockedAmount = await _repository.GetAmountOfLockedItemAsync(model.UserId, model.ItemId);

        var itemName = await _sender.Send(new GetItemNameQuery { ItemId = model.ItemId });

        if (string.IsNullOrEmpty(itemName))
            return Result<LockedItemAmountResult>.Failure("Item not found");

        return Result<LockedItemAmountResult>.Success(new LockedItemAmountResult
        {
            ItemId = model.ItemId,
            ItemName = itemName,
            Amount = lockedAmount
        });
    }

    public async Task<Result<UsersOwningItem>> GetUsersOwningThisItemAsync(GetUserIdsOwningItemQuery model) =>
        Result<UsersOwningItem>.Success(new UsersOwningItem
        {
            UserIds = await _repository.ListUsersThatOwnItemAsync(model.ItemId),
            ItemId = model.ItemId
        });

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
