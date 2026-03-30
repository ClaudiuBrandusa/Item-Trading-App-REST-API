using Domain.Aggregates.Inventories;
using Domain.Entities.Identity;
using Domain.Entities.Items;

namespace Domain.UnitTests.Aggregates;

public class InventoryTests
{
    [Fact]
    public void Constructor_InstantiateInventory_ShouldHaveTheSameUserId()
    {
        // Arrange

        string userId = User.GenerateId();

        // Act

        var inventory = new Inventory(userId);

        // Assert

        Assert.Equal(userId, inventory.UserId);
        Assert.NotNull(inventory.OwnedItems);
        Assert.Empty(inventory.OwnedItems);
    }

    [Fact]
    public async Task Constructor_AttemptToInstantiateInventoryWithInvalidUserId_ShouldThrowAnException()
    {
        // Arrange

        string userId = string.Empty;

        // Act

        var action = () =>
        {
            new Inventory(userId);
            return Task.CompletedTask;
        };

        // Assert

        await Assert.ThrowsAsync<ArgumentException>(action);
    }

    [Fact]
    public void AddItem_AddItemToInventory_ShouldHaveTheAddedItemInInventory()
    {
        // Arrange

        string userId = User.GenerateId();
        string itemId = Item.GenerateId();
        const int expectedQuantity = 5;
        var inventory = new Inventory(userId);

        // Act

        inventory.AddItem(itemId, expectedQuantity);

        // Assert

        Assert.NotNull(inventory.OwnedItems);
        Assert.Single(inventory.OwnedItems);
        var firstElement = inventory.OwnedItems.FirstOrDefault();
        Assert.NotNull(firstElement);
        Assert.Equal(itemId, firstElement.ItemId);
        Assert.Equal(expectedQuantity, firstElement.Quantity);
        var domainEvents = inventory.GetDomainEvents();
        Assert.Single(domainEvents);
        inventory.ClearDomainEvents();
        Assert.Empty(inventory.GetDomainEvents());
    }

    [Fact]
    public async Task AddItem_AttemptToAddItemWithInvalidDataToInventory_ShouldThrowAnException()
    {
        // Arrange

        string userId = User.GenerateId();
        string itemId = Item.GenerateId();
        const int expectedQuantity = 5;
        var inventory = new Inventory(userId);

        // Act

        var action1 = () =>
        {
            inventory.AddItem(string.Empty, expectedQuantity);

            return Task.CompletedTask;
        };

        var action2 = () =>
        {
            inventory.AddItem(itemId, 0);

            return Task.CompletedTask;
        };

        // Assert

        Assert.NotNull(inventory);
        await Assert.ThrowsAsync<ArgumentException>(action1);
        await Assert.ThrowsAsync<ArgumentException>(action2);
    }

    [Fact]
    public void DropItem_AddItemToInventoryAndDropTheSameAmount_ShouldRemoveTheItemFromInventory()
    {
        // Arrange

        string userId = User.GenerateId();
        string itemId = Item.GenerateId();
        const int expectedQuantity = 5;
        var inventory = new Inventory(userId);

        // Act

        inventory.AddItem(itemId, expectedQuantity);
        inventory.DropItem(itemId, expectedQuantity);

        // Assert

        Assert.NotNull(inventory.OwnedItems);
        Assert.Empty(inventory.OwnedItems);
    }

    [Fact]
    public void DropItem_AddItemToInventoryAndDropASmallerAmount_ShouldKeepTheItemInInventoryWithTheRemainedQuantity()
    {
        // Arrange

        string userId = User.GenerateId();
        string itemId = Item.GenerateId();
        const int addedQuantity = 5;
        const int droppedQuantity = 3;
        const int remainedQuantity = addedQuantity - droppedQuantity;
        var inventory = new Inventory(userId);

        // Act

        inventory.AddItem(itemId, addedQuantity);
        inventory.DropItem(itemId, droppedQuantity);

        // Assert

        Assert.NotNull(inventory.OwnedItems);
        Assert.Single(inventory.OwnedItems);
        var firstElement = inventory.OwnedItems.FirstOrDefault();
        Assert.NotNull(firstElement);
        Assert.Equal(itemId, firstElement.ItemId);
        Assert.Equal(remainedQuantity, firstElement.Quantity);
    }

    [Fact]
    public async Task DropItem_AttemptToDropItemWithoutHavingItInInventory_ShouldThrowAnException()
    {
        // Arrange

        string userId = User.GenerateId();
        string itemId = Item.GenerateId();
        string unusedItemId = Item.GenerateId();
        const int expectedQuantity = 5;
        var inventory = new Inventory(userId);

        // Act

        inventory.AddItem(itemId, expectedQuantity);

        var action1 = () =>
        {
            inventory.DropItem(string.Empty, expectedQuantity);

            return Task.CompletedTask;
        };

        var action2 = () =>
        {
            inventory.DropItem(unusedItemId, 0);

            return Task.CompletedTask;
        };

        var action3 = () =>
        {
            inventory.DropItem(unusedItemId, expectedQuantity);

            return Task.CompletedTask;
        };

        var action4 = () =>
        {
            inventory.DropItem(itemId, expectedQuantity + 2);

            return Task.CompletedTask;
        };

        // Assert

        Assert.NotNull(inventory);
        Assert.Single(inventory.OwnedItems);
        await Assert.ThrowsAsync<ArgumentException>(action1);
        await Assert.ThrowsAsync<ArgumentException>(action2);
        await Assert.ThrowsAsync<ArgumentException>(action3);
        await Assert.ThrowsAsync<ArgumentException>(action4);
    }

    [Fact]
    public async Task OwnedItems_AddSeveralInventoryItemsAndThenGetTheItemIds_ShouldReturnTheExpectedItemIds()
    {
        // Arrange

        const int itemsAmount = 5;
        const int expectedAmount = 5;
        string userId = User.GenerateId();
        var inventory = new Inventory(userId);
        var expectedItemIds = new string[itemsAmount];

        for (int i = 0; i < itemsAmount; i++)
        {
            string itemId = Item.GenerateId();

            inventory.AddItem(itemId, expectedAmount);

            expectedItemIds[i] = itemId;
        }

        // Act

        var retrievedItemIds = inventory.ItemIds.ToArray();

        // Assert

        Assert.NotNull(retrievedItemIds);
        Assert.Equal(itemsAmount, retrievedItemIds.Length);
        Assert.All(expectedItemIds, expectedItemId => retrievedItemIds.Contains(expectedItemId));
    }
}