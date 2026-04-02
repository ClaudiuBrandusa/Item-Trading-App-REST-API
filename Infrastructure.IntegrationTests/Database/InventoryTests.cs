using Domain.Aggregates.Inventories;
using Domain.Entities.Identity;
using Domain.Entities.Items;
using Domain.Repositories.Inventories;
using Infrastructure.IntegrationTests.Common;
using Infrastructure.IntegrationTests.Common.Fixtures;
using Infrastructure.IntegrationTests.Utils;
using Infrastructure.Repositories.Inventories;
using Infrastructure.Services.DatabaseContextWrapper;

namespace Infrastructure.IntegrationTests.Database;
public class InventoryTests : IClassFixture<DatabaseFixture>
{
    private readonly IInventoryRepository _repository;
    private readonly IServiceProvider _serviceProvider;

    public InventoryTests(DatabaseFixture fixture)
    {
        var dbContextWrapper = fixture.GetService<IDatabaseContextWrapper>();
        _repository = new InventoryRepository(dbContextWrapper);
        _serviceProvider = fixture.ServiceProvider!;
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

        var addInventoryResponse = await _repository.AddInventoryOrUpdateAsync(inventory);
        var inventoryResponse = await _repository.GetInventoryAsync(user.Id);
        var inventoryItem = inventoryResponse!.GetItem(item.ItemId)!;

        // Assert

        Assert.True(addInventoryResponse);
        Assert.NotNull(inventoryResponse);
        Assert.Equal(item.ItemId, inventoryItem.ItemId);
        Assert.Equal(addedQuantity, inventoryItem.Quantity);
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

        var addInventoryResponse = await _repository.AddInventoryOrUpdateAsync(inventory);
        var addSecondItemResponse = await TestingScenarios.AddItemToUser(_serviceProvider, secondItem, user, addedQuantity);
        var getInventoryResponse = await _repository.GetInventoryAsync(user.Id);
        var inventoryItem = getInventoryResponse!.GetItem(secondItem.ItemId)!;

        // Assert

        Assert.True(addInventoryResponse);
        Assert.NotNull(addSecondItemResponse);
        Assert.NotNull(getInventoryResponse);
        Assert.Equal(secondItem.ItemId, inventoryItem.ItemId);
        Assert.Equal(addedQuantity, inventoryItem.Quantity);
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

        var addItemResponse = await _repository.AddInventoryOrUpdateAsync(inventory);
        _repository.Attach(inventory);
        inventory.DropItem(item.ItemId, droppedQuantity);
        var dropItemResponse = await _repository.AddInventoryOrUpdateAsync(inventory);
        var getInventoryResponse = await _repository.GetInventoryAsync(user.Id);
        var inventoryItem = getInventoryResponse!.GetItem(item.ItemId);

        // Assert

        Assert.True(addItemResponse);
        Assert.True(dropItemResponse);
        Assert.NotNull(inventoryItem);
        Assert.Equal(item.ItemId, inventoryItem.ItemId);
        Assert.Equal(remainedItemQuantity, inventoryItem.Quantity);
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

        var addInventoryResult = await _repository.AddInventoryOrUpdateAsync(inventory);
        var inventoryResponse = await _repository.GetInventoryAsync(user.Id);

        // Assert

        Assert.True(addInventoryResult);
        Assert.NotNull(inventoryResponse);
        Assert.Equal(expectedItemsAmount, inventoryResponse.ItemIds.Count());
        Assert.All(inventory.OwnedItems, x => inventoryResponse.OwnedItems.Any(y => x.ItemId == y.ItemId));
    }

    [Fact]
    public async Task GetAmountOfFreeItem_AddItemToInventoryTheRetrieveTheAmountOfFreeItemQuantity_ShouldReturnTheExpectedQuantity()
    {
        // Arrange

        const int index = 4;

        var user = await TestingScenarios.CreateUser(_serviceProvider, $"Claudiu{index}", $"claudiu{index}@email.com", Constants.DEFAULT_USER_PASSWORD);
        var item = await TestingScenarios.CreateItemAsync(_serviceProvider, "Sandstone", "This is a resource");
        int addedQuantity = 5;
        var inventory = new Inventory(user.Id);
        inventory.AddItem(item.ItemId, addedQuantity);
        
        // Act

        var addInventoryResponse = await _repository.AddInventoryOrUpdateAsync(inventory);
        var freeItemAmount = await _repository.GetAmountOfFreeItemAsync(user.Id, item.ItemId);

        // Assert

        Assert.True(addInventoryResponse);
        Assert.Equal(addedQuantity, freeItemAmount);
    }

    [Fact]
    public async Task ListUsersThatOwnItem_AddItemToSeveralUsersAndThenListTheUsersThatOwnTheItem_RetrievesAnArrayOfUsersThatOwnTheItem()
    {
        // Arrange

        const int index = 5;

        var item = await TestingScenarios.CreateItemAsync(_serviceProvider, "Palladium", "This is a precious metal");

        int usersCount = 5;
        var users = new User[usersCount];
        var expectedQuantity = 5;

        for (int i = 0; i < usersCount; i++)
        {
            users[i] = await TestingScenarios.CreateUser(_serviceProvider, $"Claudiu_{index + i}", $"claudiu{index + i}@email.com", Constants.DEFAULT_USER_PASSWORD);
        }

        bool addItemsSucceeded = true;

        // Act

        for (int i = 0; i < usersCount; i++)
        {
            var inventory = new Inventory(users[i].Id);
            inventory.AddItem(item.ItemId, expectedQuantity);
            
            // Act

            var addItemResponse = await _repository.AddInventoryOrUpdateAsync(inventory);

            if (!addItemResponse)
            {
                addItemsSucceeded = false;
                break;
            }
        }

        var response = await _repository.ListUsersThatOwnItemAsync(item.ItemId);

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

        const int index = 11;

        var user = await TestingScenarios.CreateUser(_serviceProvider, $"Claudiu{index}", $"claudiu{index}@email.com", Constants.DEFAULT_USER_PASSWORD);
        var item = await TestingScenarios.CreateItemAsync(_serviceProvider, "Leaves", "This is a natural resource");
        int addedQuantity = 5;
        int expectedLockedAmount = 3;
        int expectedFreeAmount = addedQuantity - expectedLockedAmount;
        var inventory = new Inventory(user.Id);
        inventory.AddItem(item.ItemId, addedQuantity);

        // Act

        var addInventoryItemResponse = await _repository.AddInventoryOrUpdateAsync(inventory);
        var inventoryItem = inventory.GetItem(item.ItemId);
        inventoryItem!.Lock(expectedLockedAmount);
        var updateInventoryResult = await _repository.UpdateInventory(inventory);
        var lockedItemAmount = await _repository.GetAmountOfLockedItemAsync(user.Id, item.ItemId);
        var freeItemAmount = await _repository.GetAmountOfFreeItemAsync(user.Id, item.ItemId);

        // Assert

        Assert.True(addInventoryItemResponse);
        Assert.True(updateInventoryResult);
        Assert.Equal(expectedLockedAmount, lockedItemAmount);
        Assert.Equal(expectedFreeAmount, freeItemAmount);
    }

    [Fact]
    public async Task UnlockItem_AddItemLockAGivenAmountThenUnlockSomeAmount_RetrieveTheLockedAmount()
    {
        // Arrange

        const int index = 12;

        var user = await TestingScenarios.CreateUser(_serviceProvider, $"Claudiu{index}", $"claudiu{index}@email.com", Constants.DEFAULT_USER_PASSWORD);
        var item = await TestingScenarios.CreateItemAsync(_serviceProvider, "Glass", "This is a refined resource");
        int addedQuantity = 5;
        int lockedAmount = 3;
        int unlockAmount = lockedAmount - 1;
        int expectedFreeAmount = addedQuantity - (lockedAmount - unlockAmount);
        int expectedLockedAmount = addedQuantity - expectedFreeAmount;
        var inventory = new Inventory(user.Id);
        inventory.AddItem(item.ItemId, addedQuantity);

        // Act

        var addInventoryItemResponse = await _repository.AddInventoryOrUpdateAsync(inventory);
        var inventoryItem = inventory.GetItem(item.ItemId);
        inventoryItem!.Lock(lockedAmount);
        var lockUpdateInventoryResult = await _repository.UpdateInventory(inventory);
        inventoryItem!.Unlock(unlockAmount);
        var unlockUpdateInventoryResult = await _repository.UpdateInventory(inventory);
        var lockedItemAmount = await _repository.GetAmountOfLockedItemAsync(user.Id, item.ItemId);
        var freeItemAmount = await _repository.GetAmountOfFreeItemAsync(user.Id, item.ItemId);

        // Assert

        Assert.True(addInventoryItemResponse);
        Assert.True(lockUpdateInventoryResult);
        Assert.True(unlockUpdateInventoryResult);
        Assert.Equal(expectedLockedAmount, lockedItemAmount);
        Assert.Equal(expectedFreeAmount, freeItemAmount);
    }
}
