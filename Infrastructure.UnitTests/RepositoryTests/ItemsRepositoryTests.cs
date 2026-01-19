using Domain.Entities.Items;
using Domain.Repositories.Items;
using Infrastructure.Data;
using Infrastructure.Repositories.Items;
using Infrastructure.Services.DatabaseContextWrapper;
using Infrastructure_IntegrationTests.Utils;

namespace Infrastructure_UnitTests.RepositoryTests;

public class ItemsRepositoryTests
{
    private readonly IItemRepository _sut;

    private const string DEFAULT_ITEM_NAME = "DefaultItemName";
    private const string DEFAULT_ITEM_DESCRIPTION = "DefaultItemDescription";

    private IDatabaseContextWrapper _contextWrapper;

    public ItemsRepositoryTests()
    {
        var cacheServiceMock = TestingUtils.GetCacheServiceMock();

        _contextWrapper = TestingUtils.GetDatabaseContextWrapper(Guid.NewGuid().ToString());

        _sut = new ItemRepository(_contextWrapper);
    }

    [Fact(DisplayName = "Create a new item and return it")]
    public async Task Create_CreateANewItem_ReturnsTheCreatedItem()
    {
        // Arrange

        var itemMock = new Item(DEFAULT_ITEM_NAME, DEFAULT_ITEM_DESCRIPTION);

        // Act

        var result = await _sut.AddEntityAsync(itemMock);

        var createdItem = await _sut.GetItemEntityAsync(itemMock.ItemId);

        // Assert

        Assert.True(result, "The item should be created");
        Assert.NotNull(createdItem);
        Assert.Equal(itemMock, createdItem);
    }

    [Fact(DisplayName = "Create a new item and return it (cached)")]
    public async Task Create_CreateANewItem_ReturnsTheCachedCreatedItem()
    {
        // Arrange

        var itemMock = new Item(DEFAULT_ITEM_NAME, DEFAULT_ITEM_DESCRIPTION);

        // Act

        var result = await _sut.AddEntityAsync(itemMock);

        var createdItem = await _sut.GetItemEntityAsync(itemMock.ItemId);

        // Assert

        Assert.True(result, "The item should be created");
        Assert.NotNull(createdItem);
        Assert.Equal(itemMock, createdItem);
    }

    [Fact(DisplayName = "Update an item")]
    public async Task Update_UpdateAnItem_ReturnsTrue()
    {
        // Arrange

        var itemMock = new Item(DEFAULT_ITEM_NAME, DEFAULT_ITEM_DESCRIPTION);

        await _sut.AddEntityAsync(itemMock);

        string newName = itemMock.Name + "_NEW";

        itemMock.UpdateItemName(newName);

        // Act

        var result = await _sut.UpdateEntityAsync(itemMock);

        var entity = await _sut.GetItemEntityAsync(itemMock.ItemId);

        // Assert

        Assert.True(result, "The item should be updated");
        Assert.NotNull(entity);
        Assert.Equal(entity.Name, newName);
    }

    [Fact(DisplayName = "Will add or update an entity based upon a given condition")]
    public async Task AddOrUpdate_AddANewItem_ReturnsTrue()
    {
        // Arrange

        var itemStub = new Item(DEFAULT_ITEM_NAME, DEFAULT_ITEM_DESCRIPTION);

        // Act

        var result = await _sut.AddOrUpdateEntityAsync(itemStub, () => true);

        // Assert

        Assert.True(result, "The item should be inserted");
    }

    [Fact(DisplayName = "Will attempt to update an item that is not present in the database")]
    public async Task AddOrUpdate_UpdateAnItemThatDoesntExist_ShouldFail()
    {
        // Arrange

        var itemStub = new Item(DEFAULT_ITEM_NAME, DEFAULT_ITEM_DESCRIPTION);

        // Act

        var result = await _sut.AddOrUpdateEntityAsync(itemStub, () => false);

        // Assert

        Assert.False(result, "The item should not be updated");
    }

    [Fact(DisplayName = "Removes an item")]
    public async Task Remove_RemoveAnItem_ReturnsTrue()
    {
        // Arrange

        var itemStub = new Item(DEFAULT_ITEM_NAME, DEFAULT_ITEM_DESCRIPTION);

        var addResult = await _sut.AddEntityAsync(itemStub);

        // Act

        var result = await _sut.RemoveEntityAsync(itemStub);

        // Assert

        Assert.True(result, "Should remove the item successfully");
    }

    [Fact(DisplayName = "Removes an item")]
    public async Task Remove_RemoveAnItemThatDoesntExist_ShouldFail()
    {
        // Arrange

        var itemStub = new Item(DEFAULT_ITEM_NAME, DEFAULT_ITEM_DESCRIPTION);

        // Act

        var result = await _sut.RemoveEntityAsync(itemStub);

        // Assert

        Assert.False(result, "Should remove the item successfully");
    }

    [Fact(DisplayName = "Add a new item in the database and save the changes")]
    public async Task SaveChanged_WillSaveTheChangesInTheDatabase_ReturnsTrue()
    {
        // Arrange

        var itemStub = new Item(DEFAULT_ITEM_NAME, DEFAULT_ITEM_DESCRIPTION);

        // Act

        var dbContext = GetDatabaseContext();

        dbContext.Add(itemStub);

        int result = await _sut.SaveChangesAsync();

        // Assert

        Assert.Equal(1, result);
    }

    [Fact(DisplayName = "Add several items and then list them")]
    public async Task ListItems_AddsSeveralItemsAndListThem_ReturnsTheAddedItems()
    {
        // Arrange

        int length = 3;

        for (int i = 0; i < length; i++)
        {
            var itemStub = new Item($"{DEFAULT_ITEM_NAME}_{i}", DEFAULT_ITEM_DESCRIPTION);

            await _sut.AddEntityAsync(itemStub);
        }

        // Act

        var result = await _sut.ListItemsAsync();

        // Assert

        Assert.NotNull(result);
        Assert.Equal(length, result.Length);
    }

    [Fact(DisplayName = "Add several items and then list them (cached)")]
    public async Task ListCachedItems_AddsSeveralItemsAndListThem_ReturnsTheAddedItems()
    {
        // Arrange

        int length = 3;

        for (int i = 0; i < length; i++)
        {
            var itemStub = new Item($"{DEFAULT_ITEM_NAME}_{i}", DEFAULT_ITEM_DESCRIPTION);

            await _sut.AddEntityAsync(itemStub);
        }

        // Act

        var result = await _sut.ListItemsAsync();

        // Assert

        Assert.NotNull(result);
        Assert.Equal(length, result.Length);
    }

    #region Utils

    private DatabaseContext GetDatabaseContext()
    {
        return _contextWrapper.ProvideDatabaseContext();
    }

    #endregion Utils
}