using Domain.Aggregates.Inventory;
using Domain.Entities.Identity;
using Domain.Repositories.Inventory;
using Infrastructure.IntegrationTests.Common;
using Infrastructure.IntegrationTests.Utils;
using Infrastructure.Repositories.Inventory;
using Infrastructure.Services.DatabaseContextWrapper;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.IntegrationTests.Database;
public class InventoryTests : IClassFixture<DatabaseFixture>
{
    private readonly IInventoryRepository _repository;
    private readonly IServiceProvider _serviceProvider;

    public InventoryTests(DatabaseFixture fixture)
    {
        var dbContextWrapper = fixture.ServiceProvider.GetRequiredService<IDatabaseContextWrapper>();
        _repository = new InventoryRepository(dbContextWrapper);
        _serviceProvider = fixture.ServiceProvider;
    }

    [Fact]
    public async Task AddItem_AddsItemToInventory_ReturnsAddedItem()
    {
        // Arrange

        var user = await TestingScenarios.CreateUser(_serviceProvider, "Claudiu0", "claudiu0@email.com", Constants.DEFAULT_USER_PASSWORD);
        var item = await TestingScenarios.CreateItemAsync(_serviceProvider, "Silver", "This is a precious metal");
        int addedQuantity = 5;
        var ownedItem = new OwnedItem(item.ItemId, user.Id, addedQuantity);

        // Act

        var addItemResponse = await _repository.AddEntityAsync(ownedItem);
        var getOwnedItem = await _repository.GetOwnedItemEntityAsync(user.Id, item.ItemId);

        // Assert

        Assert.True(addItemResponse);
        Assert.NotNull(getOwnedItem);
        Assert.Equal(user.Id, getOwnedItem.UserId);
        Assert.Equal(item.ItemId, getOwnedItem.ItemId);
        Assert.Equal(addedQuantity, getOwnedItem.Quantity);
    }

    [Fact]
    public async Task DropItem_AddsItemToInventoryThenDropsAGivenItemQuantity_ReturnsTheInventoryItem()
    {
        // Arrange

        var user = await TestingScenarios.CreateUser(_serviceProvider, "Claudiu1", "claudiu1@email.com", Constants.DEFAULT_USER_PASSWORD);
        var item = await TestingScenarios.CreateItemAsync(_serviceProvider, "Bronze", "This is an alloy");
        int addedQuantity = 5;
        int droppedQuantity = 3;
        int remainedItemQuantity = addedQuantity - droppedQuantity;
        var ownedItem = new OwnedItem(item.ItemId, user.Id, addedQuantity);

        // Act

        var addItemResponse = await _repository.AddEntityAsync(ownedItem);
        var dropItemResponse = await _repository.DropItemAsync(user.Id, item.ItemId, droppedQuantity);
        var getOwnedItem = await _repository.GetOwnedItemEntityAsync(user.Id, item.ItemId);

        // Assert

        Assert.True(addItemResponse);
        Assert.NotNull(getOwnedItem);
        Assert.Equal(user.Id, getOwnedItem.UserId);
        Assert.Equal(item.ItemId, getOwnedItem.ItemId);
        Assert.Equal(remainedItemQuantity, getOwnedItem.Quantity);
    }

    [Fact]
    public async Task ListOwnedItems_AddsItemsToInventory_ListsAddedItems()
    {
        // Arrange

        var user = await TestingScenarios.CreateUser(_serviceProvider, "Claudiu2", "claudiu0@email.com", Constants.DEFAULT_USER_PASSWORD);
        var item0 = await TestingScenarios.CreateItemAsync(_serviceProvider, "Gold", "This is a precious metal");
        var item1 = await TestingScenarios.CreateItemAsync(_serviceProvider, "Iron", "This is a metal");
        var item2 = await TestingScenarios.CreateItemAsync(_serviceProvider, "Platinum", "This is a precious metal");
        int addedQuantity = 5;
        
        int ownedItemsCount = 3;
        var ownedItems = new OwnedItem[ownedItemsCount];
        ownedItems[0] = new OwnedItem(item0.ItemId, user.Id, addedQuantity);
        ownedItems[1] = new OwnedItem(item1.ItemId, user.Id, addedQuantity);
        ownedItems[2] = new OwnedItem(item2.ItemId, user.Id, addedQuantity);
        bool addItemsSucceeded = true;

        // Act

        for (int i = 0; i < ownedItemsCount; i++)
        {
            var addItemResponse = await _repository.AddEntityAsync(ownedItems[i]);
            if (!addItemResponse)
            {
                addItemsSucceeded = false;
                break;
            }
        }

        var listOwnedItems = await _repository.ListOwnedItemsAsync(user.Id);

        // Assert

        Assert.True(addItemsSucceeded);
        Assert.NotNull(listOwnedItems);
        Assert.Equal(ownedItemsCount, listOwnedItems.Length);
        Assert.All(ownedItems, x => listOwnedItems.Any(y => x == y));
    }

    [Fact]
    public async Task ListUsersThatOwnItem_AddItemToSeveralUsersAndThenListTheUsersThatOwnTheItem_RetrievesAnArrayOfUsersThatOwnTheItem()
    {
        // Arrange

        var dbFixture = await DatabaseFixture.BuildDatabaseFixture();
        var serviceProvider = dbFixture.ServiceProvider;
        var dbContextWrapper = serviceProvider.GetRequiredService<IDatabaseContextWrapper>();
        var repository = new InventoryRepository(dbContextWrapper);

        var item = await TestingScenarios.CreateItemAsync(serviceProvider, "Gold", "This is a precious metal");

        int usersCount = 5;
        var users = new User[usersCount];

        for (int i = 0; i < usersCount; i++)
        {
            users[i] = await TestingScenarios.CreateUser(serviceProvider, $"Claudiu_{i}", $"claudiu{i}@email.com", Constants.DEFAULT_USER_PASSWORD);
        }

        bool addItemsSucceeded = true;

        // Act

        for (int i = 0; i < usersCount; i++)
        {
            var addItemResponse = await repository.AddEntityAsync(new OwnedItem(item.ItemId, users[i].Id, 5));

            if (!addItemResponse)
            {
                addItemsSucceeded = false;
                break;
            }
        }

        var response = await repository.ListUsersThatOwnItemAsync(item.ItemId);

        // Assert

        Assert.True(addItemsSucceeded);
        Assert.NotNull(response);
        Assert.Equal(usersCount, response.Length);
        Assert.All(users.Select(x => x.Id).ToArray(), userId => response.Contains(userId));
    }

    [Fact]
    public async Task LockItem_AddItemLockAGivenAmount_RetrieveTheLockedAmount()
    {
        // Arrange

        var user = await TestingScenarios.CreateUser(_serviceProvider, "Claudiu3", "claudiu3@email.com", Constants.DEFAULT_USER_PASSWORD);
        var item = await TestingScenarios.CreateItemAsync(_serviceProvider, "Wood", "This is a natural resource");
        int addedQuantity = 5;
        var ownedItem = new OwnedItem(item.ItemId, user.Id, addedQuantity);
        int lockedAmount = 3;
        int expectedFreeAmount = addedQuantity - lockedAmount;

        // Act

        var addInventoryItemResponse = await _repository.AddEntityAsync(ownedItem);
        var lockItemResponse = await _repository.LockItemAsync(user.Id, item.ItemId, lockedAmount);
        var lockedItemEntityAmount = await _repository.GetLockedInventoryItemEntityAsync(user.Id, item.ItemId);
        var lockedItemAmount = await _repository.GetAmountOfLockedItemAsync(user.Id, item.ItemId);
        var freeItemAmount = await _repository.GetAmountOfFreeItemAsync(user.Id, item.ItemId);

        // Assert

        Assert.True(addInventoryItemResponse);
        Assert.True(lockItemResponse);
        Assert.NotNull(lockedItemEntityAmount);
        Assert.Equal(lockedAmount, lockedItemAmount);
        Assert.Equal(expectedFreeAmount, freeItemAmount);
        Assert.Equal(lockedAmount, lockedItemEntityAmount.Quantity);
        Assert.Equal(user.Id, lockedItemEntityAmount.UserId);
        Assert.Equal(item.ItemId, lockedItemEntityAmount.ItemId);
    }

    [Fact]
    public async Task UnlockItem_AddItemLockAGivenAmountThenUnlockSomeAmount_RetrieveTheLockedAmount()
    {
        // Arrange

        var user = await TestingScenarios.CreateUser(_serviceProvider, "Claudiu4", "claudiu4@email.com", Constants.DEFAULT_USER_PASSWORD);
        var item = await TestingScenarios.CreateItemAsync(_serviceProvider, "Glass", "This is a refined resource");
        int addedQuantity = 5;
        var ownedItem = new OwnedItem(item.ItemId, user.Id, addedQuantity);
        int lockedAmount = 3;
        int unlockAmount = lockedAmount - 1;
        int expectedFreeAmount = addedQuantity - (lockedAmount - unlockAmount);
        int expectedLockedAmount = addedQuantity - expectedFreeAmount;

        // Act

        var addInventoryItemResponse = await _repository.AddEntityAsync(ownedItem);
        var lockItemResponse = await _repository.LockItemAsync(user.Id, item.ItemId, lockedAmount);
        var unlockItemResponse = await _repository.UnlockItemAsync(user.Id, item.ItemId, unlockAmount);
        var lockedItemEntityAmount = await _repository.GetLockedInventoryItemEntityAsync(user.Id, item.ItemId);
        var lockedItemAmount = await _repository.GetAmountOfLockedItemAsync(user.Id, item.ItemId);
        var freeItemAmount = await _repository.GetAmountOfFreeItemAsync(user.Id, item.ItemId);

        // Assert

        Assert.True(addInventoryItemResponse);
        Assert.True(lockItemResponse);
        Assert.True(unlockItemResponse);
        Assert.NotNull(lockedItemEntityAmount);
        Assert.Equal(expectedLockedAmount, lockedItemAmount);
        Assert.Equal(expectedFreeAmount, freeItemAmount);
        Assert.Equal(expectedLockedAmount, lockedItemEntityAmount.Quantity);
        Assert.Equal(user.Id, lockedItemEntityAmount.UserId);
        Assert.Equal(item.ItemId, lockedItemEntityAmount.ItemId);
    }
}
