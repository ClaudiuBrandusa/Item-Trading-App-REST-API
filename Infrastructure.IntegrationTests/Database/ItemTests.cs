using Domain.Entities.Items;
using Domain.Repositories.Items;
using Infrastructure.IntegrationTests.Common.Fixtures;
using Infrastructure.Repositories.Items;
using Infrastructure.Services.DatabaseContextWrapper;

namespace Infrastructure.IntegrationTests.Database;
public class ItemTests : IClassFixture<DatabaseFixture>
{
    private readonly IItemRepository _repository;

    public ItemTests(DatabaseFixture fixture)
    {
        var dbContextWrapper = fixture.GetService<IDatabaseContextWrapper>();
        _repository = new ItemRepository(dbContextWrapper);
    }

    [Fact]
    public async Task CreateItem_ShouldCreateANewItemThenRetrieveTheItem_RetrievesTheCreatedItem()
    {
        // Arrange

        string itemName = "Gold";
        string itemDescription = "Gold is a precious metal";

        var entity = new Item(itemName, itemDescription);

        // Act

        var createResponse = await _repository.AddEntityAsync(entity);
        var getResponse = await _repository.GetItemEntityAsync(entity.ItemId);

        // Assert

        Assert.True(createResponse);
        Assert.NotNull(getResponse);
        Assert.Equal(entity.ItemId, getResponse.ItemId);
        Assert.Equal(itemName, getResponse.Name);
        Assert.Equal(itemDescription, getResponse.Description);
    }

    [Fact]
    public async Task UpdateItem_ShouldCreateANewItemUpdateTheItemThenRetrieveTheItem_RetrievesTheUpdatedItem()
    {
        // Arrange

        string itemName = "Gold";
        string itemDescription = "Gold is a precious metal";
        string updatedItemDescription = "Updated item description";

        var entity = new Item(itemName, itemDescription);

        // Act

        var createResponse = await _repository.AddEntityAsync(entity);
        var createdItemResponse = await _repository.GetItemEntityAsync(entity.ItemId);
        entity.UpdateItemDescription(updatedItemDescription);
        var updateItemResponse = await _repository.UpdateEntityAsync(entity);
        var updatedItemResponse = await _repository.GetItemEntityAsync(entity.ItemId);

        // Assert

        Assert.True(createResponse);
        Assert.NotNull(createdItemResponse);
        Assert.True(updateItemResponse);
        Assert.NotNull(updatedItemResponse);
        Assert.Equal(entity.ItemId, updatedItemResponse.ItemId);
        Assert.Equal(entity.Name, updatedItemResponse.Name);
        Assert.Equal(entity.Description, updatedItemResponse.Description);
    }

    [Fact]
    public async Task DeleteItem_ShouldCreateANewItemAndDeleteTheItemThenTryRetrievingTheItem_RetrievesNoItem()
    {
        // Arrange

        string itemName = "Gold";
        string itemDescription = "Gold is a precious metal";

        var entity = new Item(itemName, itemDescription);

        // Act

        var createResponse = await _repository.AddEntityAsync(entity);
        var deleteResponse = await _repository.RemoveEntityAsync(entity);
        var getResponse = await _repository.GetItemEntityAsync(entity.ItemId);

        // Assert

        Assert.True(createResponse);
        Assert.True(deleteResponse);
        Assert.Null(getResponse);
    }

    [Fact]
    public async Task GetItem_GetItemThatDoNotExists_ReturnsNull()
    {
        // Arrange
        string itemId = "myId";

        // Act

        var response = await _repository.GetItemEntityAsync(itemId);

        // Assert

        Assert.Null(response);
    }

    [Fact]
    public async Task ListItems_CreateABunchOfItemsThenListThem_ReturnsAListOfItemIds()
    {
        // Arrange
        var dbFixture = await DatabaseFixture.BuildDatabaseFixture();
        var dbContextWrapper = dbFixture.GetService<IDatabaseContextWrapper>();
        var repository = new ItemRepository(dbContextWrapper);

        string baseItemName = "ItemName";
        string baseItemDescription = "ItemDescription";
        int itemsQuantity = 5;

        var itemsArray = new Item[itemsQuantity];

        for (int i = 0; i < itemsQuantity; i++)
        {
            var newItem = new Item($"{baseItemName}", $"{baseItemDescription}");
            itemsArray[i] = newItem;
        }

        // Act

        bool succeeded = true;

        foreach (var entity in itemsArray)
        {
            var response = await repository.AddEntityAsync(entity);
            if (!response)
            {
                succeeded = false;
                break;
            }
        }

        var listResponse = await repository.ListItemsAsync();

        await dbFixture.DisposeAsync();
        repository.Dispose();

        // Assert
        Assert.True(succeeded);
        Assert.Equal(5, listResponse.Length);
        Assert.All(listResponse, x => itemsArray.Contains(x));
    }
}
