using CommonTestUtils.MockedServices;
using Domain.Aggregates.Inventories;
using Domain.Repositories.Inventories;
using Infrastructure.Repositories.Inventories;
using Infrastructure.Services.DatabaseContextWrapper;

namespace Infrastructure_UnitTests.RepositoryTests;

public class InventoryRepositoryTests
{
    private readonly IInventoryRepository _sut;
    private readonly string DEFAULT_USER_ID = Guid.NewGuid().ToString();
    private readonly string DEFAULT_ITEM_ID = Guid.NewGuid().ToString();

    private IDatabaseContextWrapper _contextWrapper;

    public InventoryRepositoryTests()
    {
        _contextWrapper = DatabaseUtils.GetDatabaseContextWrapper(Guid.NewGuid().ToString());

        _sut = new InventoryRepository(_contextWrapper);
    }

    [Fact(DisplayName = "Create several inventory items then list them")]
    public async Task GetInventory_CreateSeveralOwnedItemsThenRetrieveTheInventory_ReturnsInventory()
    {
        // Arrange

        int count = 5;

        var inventory = new Inventory(DEFAULT_USER_ID);

        for (int i = 0; i < count; i++)
        {
            inventory.AddItem(Guid.NewGuid().ToString(), 5 + i);
        }

        await _sut.AddEntityAsync(inventory);

        // Act

        var inventoryResult = await _sut.GetInventoryAsync(DEFAULT_USER_ID);

        // Assert

        Assert.NotNull(inventoryResult);
        Assert.Equal(count, inventoryResult.OwnedItems.Count);
    }

    [Fact(DisplayName = "Create several inventory items then list them (load inventory)")]
    public async Task LoadInventory_CreateSeveralOwnedItemsThenRetrieveTheInventory_ReturnsInventory()
    {
        // Arrange

        int count = 5;

        var inventory = new Inventory(DEFAULT_USER_ID);

        for (int i = 0; i < count; i++)
        {
            inventory.AddItem(Guid.NewGuid().ToString(), 5 + i);
        }

        await _sut.AddEntityAsync(inventory);

        // Act

        var inventoryResult = await _sut.LoadInventoryAsync(DEFAULT_USER_ID);

        // Assert

        Assert.NotNull(inventoryResult);
        Assert.Equal(count, inventoryResult.OwnedItems.Count);
    }

    [Fact(DisplayName = "Create inventory then drop item")]
    public async Task DropItem_CreateInventoryThenDropItem_ShouldSucceed()
    {
        // Arrange

        int count = 5;

        var inventory = new Inventory(DEFAULT_USER_ID);

        for (int i = 0; i < count; i++)
        {
            inventory.AddItem(Guid.NewGuid().ToString(), 5 + i);
        }

        await _sut.AddEntityAsync(inventory);

        var itemToDrop = inventory.OwnedItems.First();
        var quantityToDrop = 3;
        var expectedRemainedQuantity = itemToDrop.FreeAmount - quantityToDrop;

        // Act

        var inventoryResult = await _sut.LoadInventoryAsync(DEFAULT_USER_ID);

        inventory.DropItem(itemToDrop.ItemId, quantityToDrop);
        
        var dropItemResult = await _sut.DropItemAsync(inventory, itemToDrop.ItemId, quantityToDrop);

        

        // Assert

        Assert.NotNull(inventoryResult);

        Assert.Equal(count, inventoryResult.OwnedItems.Count);
    }

    /*[Fact(DisplayName = "Create an item then lock a given amount")]
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
    }*/
}
