using MediatR;
using Application.Services.Inventories;
using Application.Services.Notification;
using Application.Behaviors.Inventories.AddItem;
using Application.Behaviors.Inventories.DropItem;
using Application.Behaviors.Inventories.LockItem;
using Application.Behaviors.Inventories.HasItem;
using Application.Behaviors.Inventories.GetItem;
using Application.Behaviors.Inventories.ListItems;
using Application.Behaviors.Inventories.UnlockItem;
using Application.Behaviors.Inventories.GetLockedAmount;
using Application.Behaviors.Inventories.ListUsersOwningItem;
using Domain.Entities.Items;
using Domain.Entities.Inventories;
using Domain.Entities.Identity;
using Domain.Aggregates.Inventories;
using Application.Results.Items;
using Application.Repositories;

namespace Application_UnitTests.Inventories;

public class InventoryServiceTests
{
    private readonly IInventoryService _sut; // service under test
    private readonly string DEFAULT_USER_ID = User.GenerateId();
    private readonly string DEFAULT_ITEM_ID = Item.GenerateId();
    private readonly List<OwnedItem> collection;
    private readonly List<LockedItem> lockedItems;

    public InventoryServiceTests()
    {
        collection = new List<OwnedItem>();
        lockedItems = new List<LockedItem>();
        var inventoryRepositoryMock = TestingUtils.CreateRepositoryMock<OwnedItem, ICachedInventoryRepository>(collection);
        var senderMock = new Mock<ISender>();
        var publisherMock = new Mock<IPublisher>();
        var mapper = TestingUtils.GetMapper();

        #region MediatorMocks

        senderMock.Setup(x => x.Send(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
            .Returns((IRequest request, CancellationToken ct) =>
            {
                return Task.CompletedTask;
            });
        senderMock.Setup(x => x.Send(It.IsAny<IRequest<FullItemResult>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IRequest<FullItemResult> request, CancellationToken ct) =>
            {
                return new FullItemResult { ItemId = DEFAULT_ITEM_ID, ItemName = "Item", ItemDescription = "Some description", Success = true };
            });
        senderMock.Setup(x => x.Send(It.IsAny<IRequest<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IRequest<string> request, CancellationToken ct) =>
            {
                return "Item";
            });

        inventoryRepositoryMock.Setup(repo => repo.GetOwnedItemEntityAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string userId, string itemId) =>
            {
                var item = GetOwnedItem(userId, itemId);

                return item;
            });

        inventoryRepositoryMock.Setup(repo => repo.GetAmountOfLockedItemAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string userId, string itemId) =>
            {
                var lockedItem = GetLockedItem(userId, itemId);

                return lockedItem?.Quantity ?? 0;
            });

        inventoryRepositoryMock.Setup(repo => repo.GetAmountOfFreeItemAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string userId, string itemId) =>
            {
                var item = GetOwnedItem(userId, itemId);
                var lockedItem = GetLockedItem(userId, itemId);

                if (item is null) return 0;

                if (lockedItem is null) return item.Quantity;

                return item.Quantity - lockedItem.Quantity;
            });

        inventoryRepositoryMock.Setup(repo => repo.LockItemAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync((string userId, string itemId, int quantity) =>
            {
                var lockedItem = GetLockedItem(userId, itemId);

                if (lockedItem is not null)
                {
                    lockedItem.ChangeLockedAmount(lockedItem.Quantity + quantity);
                }
                else
                {
                    lockedItems.Add(new LockedItem(userId, itemId, quantity));
                }

                return true;
            });

        inventoryRepositoryMock.Setup(repo => repo.RemoveEntityAsync(It.IsAny<LockedItem>()))
            .ReturnsAsync((LockedItem item) =>
            {
                return RemoveLockedItem(item.UserId, item.ItemId);
            });

        inventoryRepositoryMock.Setup(repo => repo.UpdateEntityAsync(It.IsAny<LockedItem>()))
            .ReturnsAsync((LockedItem item) =>
            {
                return UpdateLockedItem(item.UserId, item.ItemId, item.Quantity);
            });

        inventoryRepositoryMock.Setup(repo => repo.DropItemAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync((string userId, string itemId, int amount) =>
            {
                int freeAmount = GetFreeItemAmount(userId, itemId);

                if (freeAmount < amount) return false;

                freeAmount -= amount;

                if (freeAmount == 0)
                {
                    RemoveLockedItem(userId, itemId);
                    return true;
                }
                else
                {
                    return UpdateLockedItem(userId, itemId, amount);
                }
            });

        inventoryRepositoryMock.Setup(repo => repo.UnlockItemAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync((string userId, string itemId, int quantity) =>
            {
                var lockedItem = GetLockedItem(userId, itemId);

                int remainedLockedAmount = lockedItem!.Quantity - quantity;

                if (remainedLockedAmount < 0)
                    return false;

                if (remainedLockedAmount == 0)
                {
                    return RemoveLockedItem(userId, itemId);
                }
                else
                {
                    return UpdateLockedItem(userId, itemId, remainedLockedAmount);
                }
            });

        #endregion MediatorMocks

        var clientNotificationServiceMock = new Mock<IClientNotificationService>();

        _sut = new InventoryService(inventoryRepositoryMock.Object, clientNotificationServiceMock.Object, senderMock.Object, publisherMock.Object, mapper);
    }

    [Fact(DisplayName = "Add item to inventory")]
    public async Task AddItem_AddItemToInventory_ReturnsAddedItem()
    {
        // Arrange

        int quantity = 1;

        var commandStub = new AddInventoryItemCommand
        {
            ItemId = DEFAULT_ITEM_ID,
            Quantity = quantity,
            UserId = DEFAULT_USER_ID
        };

        // Act

        var result = await _sut.AddItemAsync(commandStub);

        // Assert

        Assert.True(result.Success, "The result should be successful");
        Assert.Equal(quantity, result.Quantity);
    }

    [Theory(DisplayName = "Add item to inventory with invalid quantity")]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task AddItem_AddItemToInventoryWithInvalidQuantity_ShouldFail(int quantity)
    {
        // Arrange

        var commandStub = new AddInventoryItemCommand
        {
            ItemId = DEFAULT_ITEM_ID,
            Quantity = quantity,
            UserId = DEFAULT_USER_ID
        };

        // Act

        var result = await _sut.AddItemAsync(commandStub);

        // Assert

        Assert.False(result.Success, "The result should be unsuccessful");
        Assert.Equal(0, result.Quantity);
    }

    [Theory(DisplayName = "Drop item from inventory")]
    [InlineData(1, 1)]
    [InlineData(5, 1)]
    public async Task DropItem_DropItemFromInventory_ReturnsRemainedItem(int quantityToAdd, int quantityToDrop)
    {
        // Arrange

        var addInventoryItemCommandStub = new AddInventoryItemCommand
        {
            ItemId = DEFAULT_ITEM_ID,
            Quantity = quantityToAdd,
            UserId = DEFAULT_USER_ID
        };

        await _sut.AddItemAsync(addInventoryItemCommandStub);

        var dropInventoryItemCommandStub = new DropInventoryItemCommand
        {
            ItemId = DEFAULT_ITEM_ID,
            Quantity = quantityToDrop,
            UserId = DEFAULT_USER_ID
        };

        // Act

        var result = await _sut.DropItemAsync(dropInventoryItemCommandStub);

        // Assert

        Assert.True(result.Success == quantityToAdd >= quantityToDrop, "You should only drop the amount that is less than or equal to the amount you have");
        Assert.Equal(quantityToAdd - quantityToDrop, result.Quantity);
    }

    [Theory(DisplayName = "Drop item from inventory with invalid data")]
    [InlineData(3, 4)]
    [InlineData(5, -1)]
    [InlineData(1, 5)]
    public async Task DropItem_DropItemFromInventoryWithInvalidData_ShouldFail(int quantityToAdd, int quantityToDrop)
    {
        // Arrange

        var addInventoryItemCommandStub = new AddInventoryItemCommand
        {
            ItemId = DEFAULT_ITEM_ID,
            Quantity = quantityToAdd,
            UserId = DEFAULT_USER_ID
        };

        await _sut.AddItemAsync(addInventoryItemCommandStub);

        var dropInventoryItemCommandStub = new DropInventoryItemCommand
        {
            ItemId = DEFAULT_ITEM_ID,
            Quantity = quantityToDrop,
            UserId = DEFAULT_USER_ID
        };

        // Act

        var result = await _sut.DropItemAsync(dropInventoryItemCommandStub);

        // Assert

        Assert.False(result.Success, "You should not be able to drop more than you have");
    }

    [Theory(DisplayName = "Drop item from inventory with locked quantity")]
    [InlineData(5, 1, 1)]
    public async Task DropItem_DropItemFromInventoryWithLockedQuantity_ReturnsRemainedItem(int quantityToAdd, int quantityToDrop, int quantityToBeLocked)
    {
        // Arrange

        var addInventoryItemCommandStub = new AddInventoryItemCommand
        {
            ItemId = DEFAULT_ITEM_ID,
            Quantity = quantityToAdd,
            UserId = DEFAULT_USER_ID
        };

        await _sut.AddItemAsync(addInventoryItemCommandStub);

        var lockItemCommandStub = new LockItemCommand
        {
            ItemId = DEFAULT_ITEM_ID,
            Quantity = quantityToBeLocked,
            UserId = DEFAULT_USER_ID
        };

        await _sut.LockItemAsync(lockItemCommandStub);

        var dropInventoryItemCommandStub = new DropInventoryItemCommand
        {
            ItemId = DEFAULT_ITEM_ID,
            Quantity = quantityToDrop,
            UserId = DEFAULT_USER_ID
        };

        // Act

        var result = await _sut.DropItemAsync(dropInventoryItemCommandStub);

        int freeQuantity = quantityToAdd - quantityToBeLocked;

        // Assert

        Assert.True(result.Success == freeQuantity >= quantityToDrop, "You should only drop the amount that is less than or equal to the amount you have");
        Assert.Equal(freeQuantity - quantityToDrop, result.Quantity);
    }

    [Fact(DisplayName = "Drop item from inventory with bigger locked quantity than the held quantity")]
    public async Task DropItem_DropItemFromInventoryWithBiggerLockedQuantity_ShouldFail()
    {
        // Arrange

        int quantityToAdd = 5;
        int quantityToDrop = 1;
        int quantityToBeLocked = 5;

        var addInventoryItemCommandStub = new AddInventoryItemCommand
        {
            ItemId = DEFAULT_ITEM_ID,
            Quantity = quantityToAdd,
            UserId = DEFAULT_USER_ID
        };

        await _sut.AddItemAsync(addInventoryItemCommandStub);

        var lockItemCommandStub = new LockItemCommand
        {
            ItemId = DEFAULT_ITEM_ID,
            Quantity = quantityToBeLocked,
            UserId = DEFAULT_USER_ID
        };

        await _sut.LockItemAsync(lockItemCommandStub);

        var dropInventoryItemCommandStub = new DropInventoryItemCommand
        {
            ItemId = DEFAULT_ITEM_ID,
            Quantity = quantityToDrop,
            UserId = DEFAULT_USER_ID
        };

        // Act

        var result = await _sut.DropItemAsync(dropInventoryItemCommandStub);

        // Assert

        Assert.False(result.Success, "You should only drop the amount that is less than or equal to the amount you have");
        Assert.Equal(0, result.Quantity);
    }

    [Theory(DisplayName = "Has item inventory")]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    public async Task HasItem_HasItemInInventory_ReturnsTrue(int quantityToBeAdded, int quantityToBeChecked)
    {
        // Arrange

        var tmp = await _sut.AddItemAsync(new AddInventoryItemCommand
        {
            ItemId = DEFAULT_ITEM_ID,
            Quantity = quantityToBeAdded,
            UserId = DEFAULT_USER_ID
        });

        string item_id = tmp.ItemId;

        var queryStub = new HasItemQuantityQuery
        {
            ItemId = item_id,
            Quantity = quantityToBeChecked,
            UserId = DEFAULT_USER_ID
        };

        // Act

        var result = await _sut.HasItemAsync(queryStub);

        // Assert

        Assert.True(result, "The result should be true because the item should be in the inventory");
    }

    [Fact(DisplayName = "Has item inventory with bigger quantity to be checked")]
    public async Task HasItem_HasItemInInventoryWithBiggerQuantityToBeChecked_ReturnsFalse()
    {
        // Arrange

        int quantityToBeAdded = 1;
        int quantityToBeChecked = 2;

        var tmp = await _sut.AddItemAsync(new AddInventoryItemCommand
        {
            ItemId = DEFAULT_ITEM_ID,
            Quantity = quantityToBeAdded,
            UserId = DEFAULT_USER_ID
        });

        string itemId = tmp.ItemId;

        var queryStub = new HasItemQuantityQuery
        {
            ItemId = itemId,
            Quantity = quantityToBeChecked,
            UserId = DEFAULT_USER_ID
        };

        // Act

        var result = await _sut.HasItemAsync(queryStub);

        // Assert

        Assert.False(result, "The result should be false because the inventory does not have this quantity of this item");
    }

    [Theory(DisplayName = "Has item inventory with invalid quantity to be checked")]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    public async Task HasItem_HasItemInInventoryWithInvalidQuantityToBeChecked_ReturnsFalse(int quantityToBeAdded, int quantityToBeChecked)
    {
        // Arrange

        var tmp = await _sut.AddItemAsync(new AddInventoryItemCommand
        {
            ItemId = DEFAULT_ITEM_ID,
            Quantity = quantityToBeAdded,
            UserId = DEFAULT_USER_ID
        });

        string item_id = tmp.ItemId;

        var queryStub = new HasItemQuantityQuery
        {
            ItemId = item_id,
            Quantity = quantityToBeChecked,
            UserId = DEFAULT_USER_ID
        };

        // Act

        var result = await _sut.HasItemAsync(queryStub);

        // Assert
        
        Assert.False(result, "The result should be false because the quantity checked is invalid");
    }

    [Fact(DisplayName = "Has item inventory without adding the item in the inventory")]
    public async Task HasItem_HasItemInInventoryWithoutAddingTheItem_ReturnsFalse()
    {
        // Arrange

        var queryStub = new HasItemQuantityQuery
        {
            ItemId = DEFAULT_ITEM_ID,
            Quantity = 1,
            UserId = DEFAULT_USER_ID
        };

        // Act

        var result = await _sut.HasItemAsync(queryStub);

        // Assert

        Assert.False(result, "No item was added in the inventory, so the result should be false");
    }

    [Theory(DisplayName = "Get item from inventory")]
    [InlineData(1)]
    [InlineData(50)]
    public async Task GetItem_GetItemFromInventory_ReturnsInventoryItem(int quantityToBeAdded)
    {
        // Arrange

        var tmp = await _sut.AddItemAsync(new AddInventoryItemCommand
        {
            ItemId = DEFAULT_ITEM_ID,
            Quantity = quantityToBeAdded,
            UserId = DEFAULT_USER_ID
        });

        string item_id = tmp.ItemId;

        var queryStub = new GetInventoryItemQuery
        {
            UserId = DEFAULT_USER_ID,
            ItemId = item_id
        };

        // Act

        var result = await _sut.GetItemAsync(queryStub);

        // Assert

        Assert.True(result.Success, "The result should be successful");
        Assert.Equal(result.ItemId, DEFAULT_ITEM_ID);
    }

    [Fact(DisplayName = "Get item from inventory without adding the item in the inventory")]
    public async Task GetItem_GetItemFromInventoryWithoutAddingTheItemInTheInventory_ShouldFail()
    {
        // Arrange
        
        var queryStub = new GetInventoryItemQuery
        {
            UserId = DEFAULT_USER_ID,
            ItemId = DEFAULT_ITEM_ID
        };

        // Act

        var result = await _sut.GetItemAsync(queryStub);

        // Assert
        
        Assert.False(result.Success, "The result should be unsuccessful because the item has not been added to the inventory");
    }

    [Theory(DisplayName = "List inventory items")]
    [InlineData("1")]
    [InlineData("1", "2", "3")]
    public async Task ListItems_ListInventoryItems_ReturnsInventoryItemIdsList(params string[] itemIds)
    {
        // Arrange

        foreach (string item_id in itemIds)
            await _sut.AddItemAsync(new AddInventoryItemCommand
            {
                ItemId = item_id,
                Quantity = 1,
                UserId = DEFAULT_USER_ID
            });

        var queryStub = new ListInventoryItemsQuery
        {
            SearchString = "",
            UserId = DEFAULT_USER_ID
        };

        // Act

        var result = await _sut.ListItemsAsync(queryStub);

        // Assert

        Assert.True(result.Success, "The result should be successful");
        Assert.True(result.ItemsId.All(x => itemIds.Contains(x)), "The result should contain all of the inserted itemIds");
    }

    [Fact(DisplayName = "List inventory items without adding items")]
    public async Task ListItems_ListInventoryItemsWithoutAddingItems_ReturnsEmptyList()
    {
        // Arrange

        var queryStub = new ListInventoryItemsQuery
        {
            SearchString = "",
            UserId = DEFAULT_USER_ID
        };

        // Act

        var result = await _sut.ListItemsAsync(queryStub);

        // Assert

        Assert.True(result.Success, "The result should be successful");
        Assert.Empty(result.ItemsId);
    }

    [Fact(DisplayName = "Lock item")]
    public async Task LockItem_LockItem_ReturnsLockedItem()
    {
        // Arrange

        int quantityAdded = 1;

        await _sut.AddItemAsync(new AddInventoryItemCommand
        {
            ItemId = DEFAULT_ITEM_ID,
            Quantity = quantityAdded,
            UserId = DEFAULT_USER_ID
        });

        int quantityLocked = quantityAdded;

        var commandStub = new LockItemCommand
        {
            ItemId = DEFAULT_ITEM_ID,
            Quantity = quantityLocked,
            UserId = DEFAULT_USER_ID
        };

        // Act

        var result = await _sut.LockItemAsync(commandStub);

        // Assert

        Assert.True(result.Success, "The result should be successful");
        Assert.Equal(quantityAdded - quantityLocked, result.Quantity);
        Assert.Equal(DEFAULT_USER_ID, result.UserId);
        Assert.Equal(DEFAULT_ITEM_ID, result.ItemId);
    }

    [Fact(DisplayName = "Lock bigger quantity of item")]
    public async Task LockItem_LockBiggerQuantityOfItem_ShouldFail()
    {
        // Arrange

        int quantityAdded = 1;

        await _sut.AddItemAsync(new AddInventoryItemCommand
        {
            ItemId = DEFAULT_ITEM_ID,
            Quantity = quantityAdded,
            UserId = DEFAULT_USER_ID
        });

        var commandStub = new LockItemCommand
        {
            ItemId = DEFAULT_ITEM_ID,
            Quantity = quantityAdded + 1,
            UserId = DEFAULT_USER_ID
        };

        // Act

        var result = await _sut.LockItemAsync(commandStub);

        // Assert

        Assert.False(result.Success, "The result should be unsuccessful because it cannot lock more items than it has added");
    }

    [Theory(DisplayName = "Lock invalid quantity of item")]
    [InlineData(1, -1)]
    [InlineData(1, 0)]
    public async Task LockItem_LockInvalidQuantityOfItem_ShouldFail(int quantityAdded, int quantityLocked)
    {
        // Arrange

        await _sut.AddItemAsync(new AddInventoryItemCommand
        {
            ItemId = DEFAULT_ITEM_ID,
            Quantity = quantityAdded,
            UserId = DEFAULT_USER_ID
        });

        var commandStub = new LockItemCommand
        {
            ItemId = DEFAULT_ITEM_ID,
            Quantity = quantityLocked,
            UserId = DEFAULT_USER_ID
        };

        // Act

        var result = await _sut.LockItemAsync(commandStub);

        // Assert

        Assert.False(result.Success, "The result should be unsuccessful because the quantity to be locked is invalid");
    }

    [Fact(DisplayName = "Lock item without adding the item")]
    public async Task LockItem_LockWithoutAddingTheItem_ShouldFail()
    {
        // Arrange

        var commandStub = new LockItemCommand
        {
            ItemId = DEFAULT_ITEM_ID,
            Quantity = 1,
            UserId = DEFAULT_USER_ID
        };

        // Act

        var result = await _sut.LockItemAsync(commandStub);

        // Assert

        Assert.False(result.Success, "The result should be unsuccessful because no item has been added to the inventory");
    }

    [Theory(DisplayName = "Lock and unlock item")]
    [InlineData(1, 1, 1)]
    [InlineData(5, 4, 2)]
    public async Task UnlockItem_CreateAnItemLockAndUnlock_ReturnsTheFreeAmountOfItem(int quantityAdded, int quantityLocked, int quantityUnlocked)
    {
        // Arrange

        await _sut.AddItemAsync(new AddInventoryItemCommand
        {
            ItemId = DEFAULT_ITEM_ID,
            Quantity = quantityAdded,
            UserId = DEFAULT_USER_ID
        });
        
        await _sut.LockItemAsync(new LockItemCommand
        {
            ItemId = DEFAULT_ITEM_ID,
            Quantity = quantityLocked,
            UserId = DEFAULT_USER_ID
        });

        var commandStub = new UnlockItemCommand
        {
            ItemId = DEFAULT_ITEM_ID,
            Quantity = quantityUnlocked,
            UserId = DEFAULT_USER_ID
        };

        // Act

        var result = await _sut.UnlockItemAsync(commandStub);

        // Assert

        Assert.True(result.Success, "The result should be successful");
        Assert.Equal(quantityAdded - quantityLocked + quantityUnlocked, result.Quantity);
        Assert.Equal(DEFAULT_USER_ID, result.UserId);
        Assert.Equal(DEFAULT_ITEM_ID, result.ItemId);
    }

    [Fact(DisplayName = "Lock and unlock more than it was locked")]
    public async Task UnlockItem_UnlockMoreThanItWasLocked_ShouldFail()
    {
        // Arrange

        int quantityAdded = 1;

        await _sut.AddItemAsync(new AddInventoryItemCommand
        {
            ItemId = DEFAULT_ITEM_ID,
            Quantity = quantityAdded,
            UserId = DEFAULT_USER_ID
        });

        int quantityLocked = 1;

        await _sut.LockItemAsync(new LockItemCommand
        {
            ItemId = DEFAULT_ITEM_ID,
            Quantity = quantityLocked,
            UserId = DEFAULT_USER_ID
        });

        int quantityUnlocked = quantityLocked + 1;

        var commandStub = new UnlockItemCommand
        {
            ItemId = DEFAULT_ITEM_ID,
            Quantity = quantityUnlocked,
            UserId = DEFAULT_USER_ID
        };

        // Act

        var result = await _sut.UnlockItemAsync(commandStub);

        // Assert

        Assert.False(result.Success, "The result should be unsuccessful because you cannot unlock more than it was locked");
    }

    [Fact(DisplayName = "Lock and unlock item without adding the item")]
    public async Task UnlockItem_UnlockItemWithoutAddingTheItem_ShouldFail()
    {
        // Arrange

        int quantityLocked = 1;

        await _sut.LockItemAsync(new LockItemCommand
        {
            ItemId = DEFAULT_ITEM_ID,
            Quantity = quantityLocked,
            UserId = DEFAULT_USER_ID
        });

        int quantityUnlocked = quantityLocked;

        var commandStub = new UnlockItemCommand
        {
            ItemId = DEFAULT_ITEM_ID,
            Quantity = quantityUnlocked,
            UserId = DEFAULT_USER_ID
        };

        // Act

        var result = await _sut.UnlockItemAsync(commandStub);

        // Assert

        Assert.False(result.Success, "The result should be unsuccessful");
    }

    [Theory(DisplayName = "Get locked amount")]
    [InlineData(6, 2)]
    [InlineData(4, 4)]
    public async Task GetLockedAmount_AddAndLockItemThenGetTheLockedAmount_ReturnsTheItemsLockedAmount(int quantityAdded, int quantityLocked)
    {
        // Arrange

        await _sut.AddItemAsync(new AddInventoryItemCommand
        {
            ItemId = DEFAULT_ITEM_ID,
            Quantity = quantityAdded,
            UserId = DEFAULT_USER_ID
        });

        await _sut.LockItemAsync(new LockItemCommand
        {
            ItemId = DEFAULT_ITEM_ID,
            Quantity = quantityLocked,
            UserId = DEFAULT_USER_ID
        });

        var queryStub = new GetInventoryItemLockedAmountQuery
        {
            UserId = DEFAULT_USER_ID,
            ItemId = DEFAULT_ITEM_ID
        };

        // Act

        var result = await _sut.GetLockedAmountAsync(queryStub);

        // Assert

        Assert.True(result.Success, "The result should be successful");
        Assert.Equal(quantityLocked, result.Amount);
        Assert.Equal(DEFAULT_ITEM_ID, result.ItemId);
    }

    [Fact(DisplayName = "Get locked amount without adding the item to the inventory")]
    public async Task GetLockedAmount_GetLockedAmountWithoutAddingTheItemToTheInventory_Returns0Amount()
    {
        // Arrange

        var queryStub = new GetInventoryItemLockedAmountQuery
        {
            UserId = DEFAULT_USER_ID,
            ItemId = DEFAULT_ITEM_ID
        };

        // Act

        var result = await _sut.GetLockedAmountAsync(queryStub);

        // Assert

        Assert.Equal(0, result.Amount);
    }

    [Theory(DisplayName = "List users that own the item")]
    [InlineData("1", "2", "3")]
    [InlineData("1", "2")]
    public async Task GetUsersOwningThisItemAsync_ListUsersThatOwnTheItem_ReturnsUserIdsListOfUsersThatOwnTheItem(params string[] userIds)
    {
        // Arrange

        foreach(string userId in userIds)
        {
            await _sut.AddItemAsync(new AddInventoryItemCommand
            {
                ItemId = DEFAULT_ITEM_ID,
                UserId = userId,
                Quantity = 1
            });
        }

        var queryStub = new GetUserIdsOwningItemQuery { ItemId = DEFAULT_ITEM_ID };

        // Act

        var result = await _sut.GetUsersOwningThisItemAsync(queryStub);

        // Assert

        Assert.True(result.UserIds.All(x => userIds.Contains(x)), "The result must contain all the user ids of every user created");
    }

    #region Utils

    private OwnedItem? GetOwnedItem(string userId, string itemId) => collection.FirstOrDefault(x => x.UserId == userId && x.ItemId == itemId);

    private LockedItem? GetLockedItem(string userId, string itemId) => lockedItems.FirstOrDefault(x => x.UserId == userId && x.ItemId == itemId);

    private int GetFreeItemAmount(string userId, string itemId)
    {
        var ownedItem = GetOwnedItem(userId, itemId);
        var lockedItem = GetLockedItem(userId, itemId);

        return ownedItem.Quantity - (lockedItem?.Quantity ?? 0);
    }

    private bool UpdateLockedItem(string userId, string itemId, int amount)
    {
        var entity = GetLockedItem(userId, itemId);

        if (entity is null)
        {
            entity = new LockedItem(userId, itemId, amount);
            lockedItems.Add(entity);

            return true;
        }
        else
        {
            entity.ChangeLockedAmount(amount);
        }

        int index = lockedItems.IndexOf(entity);

        if (index == -1) return false;

        lockedItems[index] = entity;

        return true;
    }

    private bool RemoveLockedItem(string userId, string itemId)
    {
        var entity = GetLockedItem(userId, itemId);

        if (entity is null) return false;

        int index = lockedItems.IndexOf(entity);

        if (index == -1) return false;

        lockedItems.RemoveAt(index);

        return true;
    }
    
    #endregion Utils
}
