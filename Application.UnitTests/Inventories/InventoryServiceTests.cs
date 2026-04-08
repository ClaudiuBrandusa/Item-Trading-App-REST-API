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
using Domain.Entities.Identity;
using Domain.Aggregates.Inventories;
using Application.Results.Items;
using Application.Repositories;
using Application.Models.Common;
using CommonTestUtils.TestAdaptedServices;

namespace Application_UnitTests.Inventories;

public class InventoryServiceTests
{
    private readonly IInventoryService _sut; // service under test
    private readonly string DEFAULT_USER_ID = User.GenerateId();
    private readonly string DEFAULT_ITEM_ID = Item.GenerateId();
    private readonly Dictionary<string, Inventory> inventories = new();

    public InventoryServiceTests()
    {
        var inventoryRepositoryMock = RepositoryUtils.CreateRepositoryMock<Inventory, ICachedInventoryRepository>(inventories.Values.ToList());
        var senderMock = new Mock<ISender>();
        var mapper = MapperUtils.GetMapper();

        #region MediatorMocks

        senderMock.Setup(x => x.Send(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
            .Returns((IRequest request, CancellationToken ct) =>
            {
                return Task.CompletedTask;
            });
        senderMock.Setup(x => x.Send(It.IsAny<IRequest<Result<FullItemResult>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IRequest<Result<FullItemResult>> request, CancellationToken ct) =>
            {
                return Result<FullItemResult>.Success(new FullItemResult { ItemId = DEFAULT_ITEM_ID, ItemName = "Item", ItemDescription = "Some description" });
            });
        senderMock.Setup(x => x.Send(It.IsAny<IRequest<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IRequest<string> request, CancellationToken ct) =>
            {
                return "Item";
            });

        inventoryRepositoryMock.Setup(x => x.GetInventoryAsync(It.IsAny<string>()))
            .ReturnsAsync((string userId) =>
            {
                return GetInventory(userId);
            });

        inventoryRepositoryMock.Setup(x => x.LoadInventoryAsync(It.IsAny<string>()))
            .ReturnsAsync((string userId) =>
            {
                return GetInventory(userId);
            });

        inventoryRepositoryMock.Setup(x => x.AddInventoryAsync(It.IsAny<Inventory>()))
            .ReturnsAsync((Inventory inventory) =>
            {
                if (!inventories.ContainsKey(inventory.UserId))
                {
                    inventories.Add(inventory.UserId, inventory);
                }
                else
                {
                    inventories[inventory.UserId] = inventory;
                }

                return true;
            });

        inventoryRepositoryMock.Setup(repo => repo.GetAmountOfLockedItemAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string userId, string itemId) =>
            {
                var inventory = GetInventory(userId);

                if (inventory is null || !inventory.ItemIds.Contains(itemId))
                {
                    return 0;
                }

                return inventory.GetLockedItemAmount(itemId);
            });

        inventoryRepositoryMock.Setup(repo => repo.GetAmountOfFreeItemAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string userId, string itemId) =>
            {
                var inventory = GetInventory(userId);

                if (inventory is null)
                {
                    return 0;
                }

                return inventory.GetItemFreeAmount(itemId);
            });

        inventoryRepositoryMock.Setup(repo => repo.LockItemAsync(It.IsAny<Inventory>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync((Inventory inventory, string itemId, int quantity) =>
            {
                var tmp = GetInventory(inventory.UserId);
                
                return tmp is not null;
            });

        inventoryRepositoryMock.Setup(repo => repo.DropItemAsync(It.IsAny<Inventory>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync((Inventory inventory, string itemId, int amount) =>
            {
                var tmp = GetInventory(inventory.UserId);            
                return true;
            });

        inventoryRepositoryMock.Setup(repo => repo.UnlockItemAsync(It.IsAny<Inventory>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync((Inventory inventory, string itemId, int quantity) =>
            {
                var tmp = GetInventory(inventory.UserId);

                return true;
            });

        #endregion MediatorMocks

        var clientNotificationServiceMock = new Mock<IClientNotificationService>();

        _sut = new InventoryService(inventoryRepositoryMock.Object, clientNotificationServiceMock.Object, senderMock.Object, mapper);
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

        Assert.True(result.IsSuccess, "The result should be successful");
        Assert.NotNull(result.Content);
        var retrievedContent = result.Content;
        Assert.Equal(quantity, retrievedContent.Quantity);
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

        Assert.False(result.IsSuccess, "The result should be unsuccessful");
        Assert.NotEmpty(result.Error!);
        Assert.Null(result.Content);
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

        Assert.True(result.IsSuccess == quantityToAdd >= quantityToDrop, "You should only drop the amount that is less than or equal to the amount you have");
        Assert.NotNull(result.Content);
        var retrievedContent = result.Content;
        Assert.Equal(quantityToAdd - quantityToDrop, retrievedContent.Quantity);
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

        Assert.False(result.IsSuccess, "You should not be able to drop more than you have");
        Assert.NotEmpty(result.Error!);
        Assert.Null(result.Content);
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

        Assert.True(result.IsSuccess == freeQuantity >= quantityToDrop, "You should only drop the amount that is less than or equal to the amount you have");
        Assert.NotNull(result.Content);
        var retrievedContent = result.Content;
        Assert.Equal(freeQuantity - quantityToDrop, retrievedContent.Quantity);
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

        Assert.False(result.IsSuccess, "You should only drop the amount that is less than or equal to the amount you have");
        Assert.NotEmpty(result.Error!);
        Assert.Null(result.Content);
    }

    [Theory(DisplayName = "Has item inventory")]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    public async Task HasItem_HasItemInInventory_ReturnsTrue(int quantityToBeAdded, int quantityToBeChecked)
    {
        // Arrange

        var tmpResult = await _sut.AddItemAsync(new AddInventoryItemCommand
        {
            ItemId = DEFAULT_ITEM_ID,
            Quantity = quantityToBeAdded,
            UserId = DEFAULT_USER_ID
        });

        string item_id = tmpResult.Content!.ItemId;

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

        var tmpResult = await _sut.AddItemAsync(new AddInventoryItemCommand
        {
            ItemId = DEFAULT_ITEM_ID,
            Quantity = quantityToBeAdded,
            UserId = DEFAULT_USER_ID
        });

        string itemId = tmpResult.Content!.ItemId;

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

        var tmpResult = await _sut.AddItemAsync(new AddInventoryItemCommand
        {
            ItemId = DEFAULT_ITEM_ID,
            Quantity = quantityToBeAdded,
            UserId = DEFAULT_USER_ID
        });

        string item_id = tmpResult.Content!.ItemId;

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

        var tmpResult = await _sut.AddItemAsync(new AddInventoryItemCommand
        {
            ItemId = DEFAULT_ITEM_ID,
            Quantity = quantityToBeAdded,
            UserId = DEFAULT_USER_ID
        });

        string item_id = tmpResult.Content!.ItemId;

        var queryStub = new GetInventoryItemQuery
        {
            UserId = DEFAULT_USER_ID,
            ItemId = item_id
        };

        // Act

        var result = await _sut.GetItemAsync(queryStub);

        // Assert

        Assert.True(result.IsSuccess, "The result should be successful");
        Assert.NotNull(result.Content);
        var retrievedContent = result.Content;
        Assert.Equal(DEFAULT_ITEM_ID, retrievedContent.ItemId);
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
        
        Assert.False(result.IsSuccess, "The result should be unsuccessful because the item has not been added to the inventory");
        Assert.NotEmpty(result.Error!);
        Assert.Null(result.Content);
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

        Assert.True(result.IsSuccess, "The result should be successful");
        Assert.NotNull(result.Content);
        var retrievedContent = result.Content;
        Assert.True(retrievedContent.ItemsId.All(x => itemIds.Contains(x)), "The result should contain all of the inserted itemIds");
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

        Assert.True(result.IsSuccess, "The result should be successful");
        Assert.NotNull(result.Content);
        var retrievedContent = result.Content;
        Assert.Empty(retrievedContent.ItemsId);
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

        Assert.True(result.IsSuccess, "The result should be successful");
        Assert.NotNull(result.Content);
        var retrievedContent = result.Content;
        Assert.Equal(quantityAdded - quantityLocked, retrievedContent.Quantity);
        Assert.Equal(DEFAULT_USER_ID, retrievedContent.UserId);
        Assert.Equal(DEFAULT_ITEM_ID, retrievedContent.ItemId);
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

        Assert.False(result.IsSuccess, "The result should be unsuccessful because it cannot lock more items than it has added");
        Assert.NotEmpty(result.Error!);
        Assert.Null(result.Content);
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

        Assert.False(result.IsSuccess, "The result should be unsuccessful because the quantity to be locked is invalid");
        Assert.NotEmpty(result.Error!);
        Assert.Null(result.Content);
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

        Assert.False(result.IsSuccess, "The result should be unsuccessful because no item has been added to the inventory");
        Assert.NotEmpty(result.Error!);
        Assert.Null(result.Content);
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

        Assert.True(result.IsSuccess, "The result should be successful");
        Assert.NotNull(result.Content);
        var retrievedContent = result.Content;
        Assert.Equal(quantityAdded - quantityLocked + quantityUnlocked, retrievedContent.Quantity);
        Assert.Equal(DEFAULT_USER_ID, retrievedContent.UserId);
        Assert.Equal(DEFAULT_ITEM_ID, retrievedContent.ItemId);
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

        Assert.False(result.IsSuccess, "The result should be unsuccessful because you cannot unlock more than it was locked");
        Assert.NotEmpty(result.Error!);
        Assert.Null(result.Content);
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

        Assert.False(result.IsSuccess, "The result should be unsuccessful");
        Assert.NotEmpty(result.Error!);
        Assert.Null(result.Content);
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

        Assert.True(result.IsSuccess, "The result should be successful");
        Assert.NotNull(result.Content);
        var retrievedContent = result.Content;
        Assert.Equal(quantityLocked, retrievedContent.Amount);
        Assert.Equal(DEFAULT_ITEM_ID, retrievedContent.ItemId);
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

        Assert.True(result.IsSuccess);
        var retrievedContent = result.Content!;
        Assert.Equal(0, retrievedContent.Amount);
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

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Content);
        var retrievedContent = result.Content;
        Assert.True(retrievedContent.UserIds.All(x => userIds.Contains(x)), "The result must contain all the user ids of every user created");
    }

    #region Utils

    private Inventory GetInventory(string userId) => inventories.ContainsKey(userId) ? inventories[userId] : new Inventory(userId);

    #endregion Utils
}
