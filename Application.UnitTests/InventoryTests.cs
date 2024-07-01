using MediatR;
using Application.Services.Inventory;
using Application.Services.Notification;
using Application.Services.UnitOfWork;
using Application.Models.Items;
using Application.Behaviors.Inventory.AddItem;
using Application.Behaviors.Inventory.DropItem;
using Application.Behaviors.Inventory.LockItem;
using Application.Behaviors.Inventory.HasItem;
using Application.Behaviors.Inventory.GetItem;
using Application.Behaviors.Inventory.ListItems;
using Application.Behaviors.Inventory.UnlockItem;
using Application.Behaviors.Inventory.GetLockedAmount;
using Application.Behaviors.Inventory.ListUsersOwningItem;
using Domain.Repositories;
using Domain.Inventory;
using Application.Extensions;

namespace Application_UnitTests;

public class InventoryTests
{
    private readonly IInventoryService _sut; // service under test
    private readonly string userId = Guid.NewGuid().ToString();
    private readonly string itemId = Guid.NewGuid().ToString();
    private readonly List<OwnedItem> collection;
    private readonly List<LockedItem> lockedItems;

    public InventoryTests()
    {
        collection = new List<OwnedItem>();
        lockedItems = new List<LockedItem>();
        var inventoryRepositoryMock = TestingUtils.CreateRepositoryMock<OwnedItem, IInventoryRepository>(collection);
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
                return new FullItemResult { ItemId = itemId, ItemName = "Item", ItemDescription = "Some description", Success = true };
            });
        senderMock.Setup(x => x.Send(It.IsAny<IRequest<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IRequest<string> request, CancellationToken ct) =>
            {
                return "Item";
            });

        inventoryRepositoryMock.Setup(repo => repo.GetInventoryItemEntityAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string userId, string itemId) =>
            {
                var item = GetOwnedItem(userId, itemId);

                return item;
            });

        inventoryRepositoryMock.Setup(repo => repo.GetInventoryItemEntityCachedAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string userId, string itemId) =>
            {
                var item = GetOwnedItem(userId, itemId);

                if (item == null) return null;

                return mapper.AdaptToType<OwnedItem, InventoryItem>(item);
            });

        inventoryRepositoryMock.Setup(repo => repo.GetAmountOfLockedItemCachedAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string userId, string itemId) =>
            {
                var lockedItem = GetLockedItem(userId, itemId);

                return lockedItem?.Quantity ?? 0;
            });

        inventoryRepositoryMock.Setup(repo => repo.GetLockedInventoryItemEntityAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string userId, string itemId) =>
            {
                var entity = GetLockedItem(userId, itemId);

                return entity;
            });

        inventoryRepositoryMock.Setup(repo => repo.GetAmountOfFreeItemAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string userId, string itemId) =>
            {
                var item = GetOwnedItem(userId, itemId);
                var lockedItem = GetLockedItem(userId, itemId);

                if (item == null) return 0;

                if (lockedItem == null) return item.Quantity;

                return item.Quantity - lockedItem.Quantity;
            });

        inventoryRepositoryMock.Setup(repo => repo.LockItemAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync((string userId, string itemId, int quantity) =>
            {
                var lockedItem = GetLockedItem(userId, itemId);

                if (lockedItem != null)
                {
                    lockedItem.Quantity += quantity;
                }
                else
                {
                    lockedItems.Add(new LockedItem { UserId = userId, ItemId = itemId, Quantity = quantity });
                }

                return true;
            });

        inventoryRepositoryMock.Setup(repo => repo.RemoveEntityAsync(It.IsAny<LockedItem>()))
            .ReturnsAsync((LockedItem item) =>
            {
                var entity = GetLockedItem(item.UserId, item.ItemId);

                if (entity == null) return false;

                int index = lockedItems.IndexOf(entity);

                if (index == -1) return false;

                lockedItems.RemoveAt(index);

                return true;
            });

        inventoryRepositoryMock.Setup(repo => repo.UpdateEntityAsync(It.IsAny<LockedItem>()))
            .ReturnsAsync((LockedItem item) =>
            {
                var entity = GetLockedItem(item.UserId, item.ItemId);

                if (entity == null) return false;

                int index = lockedItems.IndexOf(entity);

                if (index == -1) return false;

                lockedItems[index] = entity;

                return true;
            });

        #endregion MediatorMocks

        var cacheServiceMock = TestingUtils.GetCacheServiceMock();
        var clientNotificationServiceMock = new Mock<IClientNotificationService>();
        var unitOfWorkMock = new Mock<IUnitOfWorkService>();

        _sut = new InventoryService(inventoryRepositoryMock.Object, clientNotificationServiceMock.Object, cacheServiceMock.Object, senderMock.Object, publisherMock.Object, mapper, unitOfWorkMock.Object);
    }

    [Fact(DisplayName = "Add item to inventory")]
    public async Task AddItem_AddItemToInventory_ReturnsAddedItem()
    {
        // Arrange

        int quantity = 1;

        var commandStub = new AddInventoryItemCommand
        {
            ItemId = itemId,
            Quantity = quantity,
            UserId = userId
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
            ItemId = itemId,
            Quantity = quantity,
            UserId = userId
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
            ItemId = itemId,
            Quantity = quantityToAdd,
            UserId = userId
        };

        await _sut.AddItemAsync(addInventoryItemCommandStub);

        var dropInventoryItemCommandStub = new DropInventoryItemCommand
        {
            ItemId = itemId,
            Quantity = quantityToDrop,
            UserId = userId
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
            ItemId = itemId,
            Quantity = quantityToAdd,
            UserId = userId
        };

        await _sut.AddItemAsync(addInventoryItemCommandStub);

        var dropInventoryItemCommandStub = new DropInventoryItemCommand
        {
            ItemId = itemId,
            Quantity = quantityToDrop,
            UserId = userId
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
            ItemId = itemId,
            Quantity = quantityToAdd,
            UserId = userId
        };

        await _sut.AddItemAsync(addInventoryItemCommandStub);

        var lockItemCommandStub = new LockItemCommand
        {
            ItemId = itemId,
            Quantity = quantityToBeLocked,
            UserId = userId
        };

        await _sut.LockItemAsync(lockItemCommandStub);

        var dropInventoryItemCommandStub = new DropInventoryItemCommand
        {
            ItemId = itemId,
            Quantity = quantityToDrop,
            UserId = userId
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
            ItemId = itemId,
            Quantity = quantityToAdd,
            UserId = userId
        };

        await _sut.AddItemAsync(addInventoryItemCommandStub);

        var lockItemCommandStub = new LockItemCommand
        {
            ItemId = itemId,
            Quantity = quantityToBeLocked,
            UserId = userId
        };

        await _sut.LockItemAsync(lockItemCommandStub);

        var dropInventoryItemCommandStub = new DropInventoryItemCommand
        {
            ItemId = itemId,
            Quantity = quantityToDrop,
            UserId = userId
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
            ItemId = itemId,
            Quantity = quantityToBeAdded,
            UserId = userId
        });

        string item_id = tmp.ItemId;

        var queryStub = new HasItemQuantityQuery
        {
            ItemId = item_id,
            Quantity = quantityToBeChecked,
            UserId = userId
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
            ItemId = itemId,
            Quantity = quantityToBeAdded,
            UserId = userId
        });

        string item_id = tmp.ItemId;

        var queryStub = new HasItemQuantityQuery
        {
            ItemId = item_id,
            Quantity = quantityToBeChecked,
            UserId = userId
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
            ItemId = itemId,
            Quantity = quantityToBeAdded,
            UserId = userId
        });

        string item_id = tmp.ItemId;

        var queryStub = new HasItemQuantityQuery
        {
            ItemId = item_id,
            Quantity = quantityToBeChecked,
            UserId = userId
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
            ItemId = itemId,
            Quantity = 1,
            UserId = userId
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
            ItemId = itemId,
            Quantity = quantityToBeAdded,
            UserId = userId
        });

        string item_id = tmp.ItemId;

        var queryStub = new GetInventoryItemQuery
        {
            UserId = userId,
            ItemId = item_id
        };

        // Act

        var result = await _sut.GetItemAsync(queryStub);

        // Assert

        Assert.True(result.Success, "The result should be successful");
        Assert.Equal(result.ItemId, itemId);
    }

    [Fact(DisplayName = "Get item from inventory without adding the item in the inventory")]
    public async Task GetItem_GetItemFromInventoryWithoutAddingTheItemInTheInventory_ShouldFail()
    {
        // Arrange
        
        var queryStub = new GetInventoryItemQuery
        {
            UserId = userId,
            ItemId = itemId
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
                UserId = userId
            });

        var queryStub = new ListInventoryItemsQuery
        {
            SearchString = "",
            UserId = userId
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
            UserId = userId
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
            ItemId = itemId,
            Quantity = quantityAdded,
            UserId = userId
        });

        int quantityLocked = quantityAdded;

        var commandStub = new LockItemCommand
        {
            ItemId = itemId,
            Quantity = quantityLocked,
            UserId = userId
        };

        // Act

        var result = await _sut.LockItemAsync(commandStub);

        // Assert

        Assert.True(result.Success, "The result should be successful");
        Assert.Equal(quantityLocked, result.Quantity);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(itemId, result.ItemId);
    }

    [Fact(DisplayName = "Lock bigger quantity of item")]
    public async Task LockItem_LockBiggerQuantityOfItem_ShouldFail()
    {
        // Arrange

        int quantityAdded = 1;

        await _sut.AddItemAsync(new AddInventoryItemCommand
        {
            ItemId = itemId,
            Quantity = quantityAdded,
            UserId = userId
        });

        var commandStub = new LockItemCommand
        {
            ItemId = itemId,
            Quantity = quantityAdded + 1,
            UserId = userId
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
            ItemId = itemId,
            Quantity = quantityAdded,
            UserId = userId
        });

        var commandStub = new LockItemCommand
        {
            ItemId = itemId,
            Quantity = quantityLocked,
            UserId = userId
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
            ItemId = itemId,
            Quantity = 1,
            UserId = userId
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
            ItemId = itemId,
            Quantity = quantityAdded,
            UserId = userId
        });
        
        await _sut.LockItemAsync(new LockItemCommand
        {
            ItemId = itemId,
            Quantity = quantityLocked,
            UserId = userId
        });

        var commandStub = new UnlockItemCommand
        {
            ItemId = itemId,
            Quantity = quantityUnlocked,
            UserId = userId
        };

        // Act

        var result = await _sut.UnlockItemAsync(commandStub);

        // Assert

        Assert.True(result.Success, "The result should be successful");
        Assert.Equal(quantityLocked - quantityUnlocked, result.Quantity);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(itemId, result.ItemId);
    }

    [Fact(DisplayName = "Lock and unlock more than it was locked")]
    public async Task UnlockItem_UnlockMoreThanItWasLocked_ShouldFail()
    {
        // Arrange

        int quantityAdded = 1;

        await _sut.AddItemAsync(new AddInventoryItemCommand
        {
            ItemId = itemId,
            Quantity = quantityAdded,
            UserId = userId
        });

        int quantityLocked = 1;

        await _sut.LockItemAsync(new LockItemCommand
        {
            ItemId = itemId,
            Quantity = quantityLocked,
            UserId = userId
        });

        int quantityUnlocked = quantityLocked + 1;

        var commandStub = new UnlockItemCommand
        {
            ItemId = itemId,
            Quantity = quantityUnlocked,
            UserId = userId
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
            ItemId = itemId,
            Quantity = quantityLocked,
            UserId = userId
        });

        int quantityUnlocked = quantityLocked;

        var commandStub = new UnlockItemCommand
        {
            ItemId = itemId,
            Quantity = quantityUnlocked,
            UserId = userId
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
            ItemId = itemId,
            Quantity = quantityAdded,
            UserId = userId
        });

        await _sut.LockItemAsync(new LockItemCommand
        {
            ItemId = itemId,
            Quantity = quantityLocked,
            UserId = userId
        });

        var queryStub = new GetInventoryItemLockedAmountQuery
        {
            UserId = userId,
            ItemId = itemId
        };

        // Act

        var result = await _sut.GetLockedAmountAsync(queryStub);

        // Assert

        Assert.True(result.Success, "The result should be successful");
        Assert.Equal(quantityLocked, result.Amount);
        Assert.Equal(itemId, result.ItemId);
    }

    [Fact(DisplayName = "Get locked amount without adding the item to the inventory")]
    public async Task GetLockedAmount_GetLockedAmountWithoutAddingTheItemToTheInventory_Returns0Amount()
    {
        // Arrange

        var queryStub = new GetInventoryItemLockedAmountQuery
        {
            UserId = userId,
            ItemId = itemId
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
                ItemId = itemId,
                UserId = userId,
                Quantity = 1
            });
        }

        var queryStub = new GetUserIdsOwningItemQuery { ItemId = itemId };

        // Act

        var result = await _sut.GetUsersOwningThisItemAsync(queryStub);

        // Assert

        Assert.True(result.UserIds.All(x => userIds.Contains(x)), "The result must contain all the user ids of every user created");
    }

    #region Utils

    private OwnedItem? GetOwnedItem(string userId, string itemId) => collection.FirstOrDefault(x => x.UserId == userId && x.ItemId == itemId);

    private LockedItem? GetLockedItem(string userId, string itemId) => lockedItems.FirstOrDefault(x => x.UserId == userId && x.ItemId == itemId);

    #endregion Utils
}
