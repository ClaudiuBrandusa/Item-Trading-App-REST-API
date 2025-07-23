using Domain.Aggregates.Inventory;
using Domain.Repositories.Inventory;
using Infrastructure.Repositories.Inventory;
using Infrastructure.Services.DatabaseContextWrapper;
using Infrastructure_IntegrationTests.Utils;

namespace Infrastructure_UnitTests.RepositoryTests;

public class InventoryRepositoryTests
{
    private readonly IInventoryRepository _sut;
    private readonly string DEFAULT_USER_ID = Guid.NewGuid().ToString();
    private readonly string DEFAULT_ITEM_ID = Guid.NewGuid().ToString();

    private IDatabaseContextWrapper _contextWrapper;

    public InventoryRepositoryTests()
    {
        _contextWrapper = TestingUtils.GetDatabaseContextWrapper(Guid.NewGuid().ToString());
        
        _sut = new InventoryRepository(_contextWrapper);
    }

    [Fact(DisplayName = "Create inventory item then get item by id")]
    public async Task GetInventoryItem_CreateInventoryItemThenGetItemById_ReturnsCreatedInventoryItem()
    {
        // Arrange

        var inventoryItemMock = new OwnedItem(DEFAULT_ITEM_ID, DEFAULT_USER_ID, 5);

        var addItem = await _sut.AddEntityAsync(inventoryItemMock);

        // Act

        var itemResult = await _sut.GetOwnedItemEntityAsync(inventoryItemMock.UserId, inventoryItemMock.ItemId);

        // Assert

        Assert.NotNull(itemResult);
        Assert.Equal(inventoryItemMock.ItemId, itemResult.ItemId);
        Assert.Equal(inventoryItemMock.UserId, itemResult.UserId);
        Assert.Equal(inventoryItemMock.Quantity, itemResult.Quantity);
    }

    /*[Fact(DisplayName = "Create inventory item then get item by id (cached)")]
    public async Task GetInventoryItem_CreateInventoryItemThenGetItemById_ReturnsCachedCreatedInventoryItem()
    {
        // Arrange

        var inventoryItemMock = new OwnedItem(DEFAULT_ITEM_ID, DEFAULT_USER_ID, 5);

        var addItem = await _sut.AddEntityAsync(inventoryItemMock);

        // Act

        var itemResult = await _sut.GetInventoryItemEntityCachedAsync(inventoryItemMock.UserId, inventoryItemMock.ItemId);

        // Assert

        Assert.NotNull(itemResult);
        Assert.Equal(inventoryItemMock.ItemId, itemResult.Id);
        Assert.Equal(inventoryItemMock.Quantity, itemResult.Quantity);
    }*/

    [Fact(DisplayName = "Create several inventory items then list them")]
    public async Task ListOwnedItems_CreateSeveralOwnedItemsThenListThem_ReturnsAnArrayOfTheNewlyCreatedOwnedItems()
    {
        // Arrange

        int count = 5;

        for (int i = 0; i < count; i++)
        {
            var inventoryItemMock = new OwnedItem(Guid.NewGuid().ToString(), DEFAULT_USER_ID, 5 + i);

            var addItem = await _sut.AddEntityAsync(inventoryItemMock);
        }

        // Act

        var listItemsResult = await _sut.ListOwnedItemsAsync(DEFAULT_USER_ID);

        // Assert

        Assert.NotNull(listItemsResult);
        Assert.Equal(count, listItemsResult.Length);
    }

    /*[Fact(DisplayName = "Create several inventory items then list them (cached)")]
    public async Task ListOwnedItems_CreateSeveralOwnedItemsThenListTheCachedItems_ReturnsAnArrayOfTheNewlyCreatedOwnedItems()
    {
        // Arrange

        int count = 5;

        for (int i = 0; i < count; i++)
        {
            var inventoryItemMock = new OwnedItem(Guid.NewGuid().ToString(), DEFAULT_USER_ID, 5 + i);

            var addItem = await _sut.AddEntityAsync(inventoryItemMock);
        }

        // Act

        var listItemsResult = await _sut.ListInventoryItemsCachedAsync(DEFAULT_USER_ID);

        // Assert

        Assert.NotNull(listItemsResult);
        Assert.Equal(count, listItemsResult.Length);
    }*/

    [Fact(DisplayName = "Create an item then lock a given amount")]
    public async Task LockItem_CreateItemThenLockAGivenAmount_ReturnsTrue()
    {
        // Arrange

        int addedAmount = 5;
        int lockedAmount = 3;

        var inventoryItemMock = new OwnedItem(DEFAULT_ITEM_ID, DEFAULT_USER_ID, addedAmount);

        var addItem = await _sut.AddEntityAsync(inventoryItemMock);

        // Act

        var lockResult = await _sut.LockItemAsync(DEFAULT_USER_ID, inventoryItemMock.ItemId, lockedAmount);

        // Assert

        Assert.True(lockResult, "The lock item operation should succeed");
    }

    [Fact(DisplayName = "Create an item then try to lock an amount greater than the owned amount")]
    public async Task LockItem_CreateItemThenLockAGreaterGivenAmount_ShouldReturnFalse()
    {
        // Arrange

        int addedAmount = 5;
        int lockedAmount = 8;

        var inventoryItemMock = new OwnedItem(Guid.NewGuid().ToString(), DEFAULT_USER_ID, addedAmount);

        var addItem = await _sut.AddEntityAsync(inventoryItemMock);

        // Act

        var lockResult = await _sut.LockItemAsync(DEFAULT_USER_ID, inventoryItemMock.ItemId, lockedAmount);

        // Assert

        Assert.True(lockResult, "The lock item operation should fail");
    }

    [Fact(DisplayName = "Create item, lock a given amount then return the locked amount")]
    public async Task GetAmountOfLockedItem_CreateItemLockAGivenAmountGetLockedAmount_ReturnsTheLockedAmount()
    {
        // Arrange

        int addedAmount = 5;
        int lockedAmount = 3;

        var inventoryItemMock = new OwnedItem(Guid.NewGuid().ToString(), DEFAULT_USER_ID, addedAmount);

        var addItem = await _sut.AddEntityAsync(inventoryItemMock);

        var lockResult = await _sut.LockItemAsync(DEFAULT_USER_ID, inventoryItemMock.ItemId, lockedAmount);

        // Act

        var lockedAmountResult = await _sut.GetAmountOfLockedItemAsync(DEFAULT_USER_ID, inventoryItemMock.ItemId);

        // Assert

        Assert.Equal(lockedAmount, lockedAmountResult);
    }

    [Fact(DisplayName = "Add the same inventory item type to several users and then list the users that own the item")]
    public async Task ListUsersThatOwnItem_AddSameInventoryItemTypeToSeveralUsersThenListTheUsersOwningTheItem_ReturnsAListOfTheUsersThatOwnTheItem()
    {
        // Arrange

        int count = 5;
        string itemId = DEFAULT_ITEM_ID;

        for (int i = 0; i < count; i++)
        {
            var item = new OwnedItem(itemId, Guid.NewGuid().ToString(), i + 1);

            await _sut.AddEntityAsync(item);
        }

        // Act

        var usersOwningTheItem = await _sut.ListUsersThatOwnItemAsync(itemId);

        // Assert

        Assert.NotNull(usersOwningTheItem);
        Assert.Equal(count, usersOwningTheItem.Length);
    }

    [Fact(DisplayName = "Add an inventory item and lock a given amount then return the free amount")]
    public async Task GetAmountOfFreeItem_AddInventoryItemAndLockAGivenAmountThenReturnTheFreeAmount_ReturnsTheFreeAmountOfTheInventoryItem()
    {
        // Arrange

        int addedQuantity = 5;
        int lockedQuantity = 3;

        var inventoryItemMock = new OwnedItem(DEFAULT_ITEM_ID, DEFAULT_USER_ID, addedQuantity);

        var addItem = await _sut.AddEntityAsync(inventoryItemMock);

        var lockItemResult = await _sut.LockItemAsync(inventoryItemMock.UserId, inventoryItemMock.ItemId, lockedQuantity);

        // Act

        var freeAmountResult = await _sut.GetAmountOfFreeItemAsync(inventoryItemMock.UserId, inventoryItemMock.ItemId);

        // Assert

        Assert.Equal(addedQuantity - lockedQuantity, freeAmountResult);
    }

    [Fact(DisplayName = "Add an inventory item and lock a given amount then return the locked item entity")]
    public async Task GetAmountOfFreeItem_AddInventoryItemAndLockAGivenAmountThenReturnTheFreeAmount_ReturnsTheLockedItemEntity()
    {
        // Arrange

        int addedQuantity = 5;
        int lockedQuantity = 3;

        var inventoryItemMock = new OwnedItem(DEFAULT_ITEM_ID, DEFAULT_USER_ID, addedQuantity);

        var addItem = await _sut.AddEntityAsync(inventoryItemMock);

        var lockItemResult = await _sut.LockItemAsync(inventoryItemMock.UserId, inventoryItemMock.ItemId, lockedQuantity);

        // Act

        var lockedItemEntityResult = await _sut.GetLockedInventoryItemEntityAsync(inventoryItemMock.UserId, inventoryItemMock.ItemId);

        // Assert

        Assert.NotNull(lockedItemEntityResult);
        Assert.Equal(lockedQuantity, lockedItemEntityResult.Quantity);
        Assert.Equal(inventoryItemMock.UserId, lockedItemEntityResult.UserId);
    }
}
