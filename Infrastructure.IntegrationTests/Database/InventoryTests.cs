using Domain.Aggregates.Inventories;
using Domain.Entities.Items;
using Domain.Repositories.Inventories;
using Infrastructure.IntegrationTests.Common;
using Infrastructure.IntegrationTests.Utils;
using Infrastructure.Repositories.Inventories;
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
    public async Task AddInventory_AddsItemToInventory_ReturnsAddedItem()
    {
        // Arrange

        const int index = 0;

        var user = await TestingScenarios.CreateUser(_serviceProvider, $"Claudiu{index}", $"claudiu{index}@email.com", Constants.DEFAULT_USER_PASSWORD);
        var item = await TestingScenarios.CreateItemAsync(_serviceProvider, "Silver", "This is a precious metal");
        int addedQuantity = 5;
        var inventory = new Inventory(user.Id);
        inventory.AddItem(item.ItemId, addedQuantity);
        
        // Act

        var addInventoryResponse = await _repository.AddInventoryAsync(inventory);
        var getInventoryItem = await _repository.GetOwnedItemEntityAsync(user.Id, item.ItemId);

        // Assert

        Assert.True(addInventoryResponse);
        Assert.NotNull(getInventoryItem);
        Assert.Equal(item.ItemId, getInventoryItem.ItemId);
        Assert.Equal(addedQuantity, getInventoryItem.Quantity);
    }

    [Fact]
    public async Task AddInventory_AddsItemToInventoryThenAddAnotherItemThroughSeparateRepository_ReturnsAddedItems()
    {
        // Arrange

        const int index = 1;

        var user = await TestingScenarios.CreateUser(_serviceProvider, $"Claudiu{index}", $"claudiu{index}@email.com", Constants.DEFAULT_USER_PASSWORD);
        var firstItem = await TestingScenarios.CreateItemAsync(_serviceProvider, "Platinum", "This is a precious metal");
        var secondItem = await TestingScenarios.CreateItemAsync(_serviceProvider, "Wood", "This is a resource");
        int addedQuantity = 5;
        var inventory = new Inventory(user.Id);
        inventory.AddItem(firstItem.ItemId, addedQuantity);

        // Act

        var addInventoryResponse = await _repository.AddInventoryAsync(inventory);
        var addSecondItemResponse = await TestingScenarios.AddItemToUser(_serviceProvider, secondItem, user, addedQuantity);
        var secondInventoryItemResponse = await _repository.GetOwnedItemEntityAsync(user.Id, secondItem.ItemId);

        // Assert

        Assert.True(addInventoryResponse);
        Assert.NotNull(addSecondItemResponse);
        Assert.NotNull(secondInventoryItemResponse);
        Assert.Equal(secondItem.ItemId, secondInventoryItemResponse.ItemId);
        Assert.Equal(addedQuantity, secondInventoryItemResponse.Quantity);
    }

    [Fact]
    public async Task DropItem_AddsItemToInventoryThenDropsAGivenItemQuantity_ReturnsTheInventoryItem()
    {
        // Arrange

        const int index = 2;

        var user = await TestingScenarios.CreateUser(_serviceProvider, $"Claudiu{index}", $"claudiu{index}@email.com", Constants.DEFAULT_USER_PASSWORD);
        var item = await TestingScenarios.CreateItemAsync(_serviceProvider, "Bronze", "This is an alloy");
        int addedQuantity = 5;
        int droppedQuantity = 3;
        int remainedItemQuantity = addedQuantity - droppedQuantity;
        var inventory = new Inventory(user.Id);
        inventory.AddItem(item.ItemId, addedQuantity);
        
        // Act

        var addItemResponse = await _repository.AddInventoryAsync(inventory);
        var dropItemResponse = await _repository.DropItemAsync(user.Id, item.ItemId, droppedQuantity);
        var getOwnedItem = await _repository.GetOwnedItemEntityAsync(user.Id, item.ItemId);

        // Assert

        Assert.True(addItemResponse);
        Assert.NotNull(getOwnedItem);
        Assert.Equal(item.ItemId, getOwnedItem.ItemId);
        Assert.Equal(remainedItemQuantity, getOwnedItem.Quantity);
    }

    [Fact]
    public async Task GetInventory_AddsItemsToInventory_ListsAddedItems()
    {
        // Arrange

        const int index = 3;

        var user = await TestingScenarios.CreateUser(_serviceProvider, $"Claudiu{index}", $"claudiu{index}@email.com", Constants.DEFAULT_USER_PASSWORD);

        const int expectedItemsAmount = 3;

        var itemNamesAndDescriptions = new (string Name, string Description)[]
        {
            ("Gold", "This is a precious metal"),
            ("Iron", "This is a metal"),
            ("Clay", "This is a resource"),
        };

        var items = new Item[expectedItemsAmount];

        for (int i = 0; i < expectedItemsAmount; i++)
        {
            items[i] = await TestingScenarios.CreateItemAsync(_serviceProvider, itemNamesAndDescriptions[i].Name, itemNamesAndDescriptions[i].Description);
        }

        const int addedQuantity = 5;

        var inventory = new Inventory(user.Id);

        for (int i = 0; i < expectedItemsAmount; i++)
        {
            inventory.AddItem(items[i].ItemId, addedQuantity);
        }

        // Act

        var addInventoryResult = await _repository.AddInventoryAsync(inventory);

        var inventoryResponse = await _repository.GetInventoryAsync(user.Id);

        // Assert

        Assert.True(addInventoryResult);
        Assert.NotNull(inventoryResponse);
        Assert.Equal(expectedItemsAmount, inventoryResponse.ItemIds.Count());
        Assert.All(inventory.OwnedItems, x => inventoryResponse.OwnedItems.Any(y => x.ItemId == y.ItemId));
    }

    /*[Fact]
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
    }*/

    /*[Fact]
    public async Task LockItem_AddItemLockAGivenAmount_RetrieveTheLockedAmount()
    {
        // Arrange

        const int index = 4;

        var user = await TestingScenarios.CreateUser(_serviceProvider, $"Claudiu{index}", $"claudiu{index}@email.com", Constants.DEFAULT_USER_PASSWORD);
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
    }*/

    /*[Fact]
    public async Task UnlockItem_AddItemLockAGivenAmountThenUnlockSomeAmount_RetrieveTheLockedAmount()
    {
        // Arrange

        const int index = 5;

        var user = await TestingScenarios.CreateUser(_serviceProvider, $"Claudiu{index}", $"claudiu{index}@email.com", Constants.DEFAULT_USER_PASSWORD);
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
    }*/
}
