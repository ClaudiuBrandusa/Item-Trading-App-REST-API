using Application.Behaviors.Inventories.ListUsersOwningItem;
using Application.Behaviors.Item.CreateItem;
using Application.Behaviors.Item.DeleteItem;
using Application.Behaviors.Item.GetItem;
using Application.Behaviors.Item.GetItemDescription;
using Application.Behaviors.Item.GetItemName;
using Application.Behaviors.Item.ListItems;
using Application.Behaviors.Item.UpdateItem;
using Application.Models.Inventories;
using Application.Services.Item;
using Domain.Entities.Identity;
using Domain.Entities.Items;
using Application.Repositories;
using MediatR;

namespace Application_UnitTests.Items;

public class ItemServiceTests
{
    private readonly IItemService _sut; // service under test
    private readonly string DEFAULT_USER_ID = User.GenerateId();
    private const string DEFAULT_ITEM_NAME = "Item_Name";
    private const string DEFAULT_ITEM_DESCRIPTION = "Item_Description";
    private readonly List<Item> collection;

    public ItemServiceTests()
    {
        collection = new List<Item>();
        var itemRepositoryMock = TestingUtils.CreateRepositoryMock<Item, ICachedItemRepository>(collection);
        var senderMock = new Mock<ISender>();
        var publisherMock = new Mock<IPublisher>();
        var cacheServiceMock = TestingUtils.GetCacheServiceMock();

        #region MediatorMocks

        senderMock.Setup(x => x.Send(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
            .Returns((IRequest request, CancellationToken ct) =>
            {
                return Task.CompletedTask;
            });

        senderMock.Setup(x => x.Send(It.IsAny<IRequest<UsersOwningItem>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IRequest<UsersOwningItem> request, CancellationToken ct) =>
            {
                return new UsersOwningItem { ItemId = ((GetUserIdsOwningItemQuery)request).ItemId};
            });

        itemRepositoryMock.Setup(repo => repo.GetItemEntityAsync(It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync((string itemId, bool setCache) =>
            {
                return collection.FirstOrDefault(x => x.ItemId == itemId);
            });

        itemRepositoryMock.Setup(repo => repo.ListItemsAsync())
            .ReturnsAsync(() => collection.ToArray());

        #endregion MediatorMocks

        _sut = new ItemService(itemRepositoryMock.Object, senderMock.Object, publisherMock.Object, TestingUtils.GetMapper());
    }

    [Fact(DisplayName = "Create a new item")]
    public async Task CreateItem_CreateNewItem_ReturnsCreatedItem()
    {
        // Arrange

        var commandStub = new CreateItemCommand
        {
            SenderUserId = DEFAULT_USER_ID,
            ItemName = DEFAULT_ITEM_NAME,
            ItemDescription = DEFAULT_ITEM_DESCRIPTION
        };

        // Act 

        // add one item
        var addItemResult = await _sut.CreateItemAsync(commandStub);

        // Assert

        Assert.True(addItemResult.Success, "The item creation should be successful");
    }

    [Fact(DisplayName = "Create a new item without sender user id")]
    public async Task CreateItem_CreateNewItemWithoutSenderUserId_ShouldFail()
    {
        // Arrange

        var commandStub = new CreateItemCommand
        {
            SenderUserId = "",
            ItemName = DEFAULT_ITEM_NAME,
            ItemDescription = DEFAULT_ITEM_DESCRIPTION
        };

        // Act

        // add one item
        var addItemResult = await _sut.CreateItemAsync(commandStub);

        // Assert

        Assert.False(addItemResult.Success, "It should not be allowed to create an item without a sender user id");
    }

    [Fact(DisplayName = "Create a new item without a name")]
    public async Task CreateItem_CreateNewItemWithoutName_ShouldFail()
    {
        // Arrange

        var commandStub = new CreateItemCommand
        {
            SenderUserId = DEFAULT_USER_ID,
            ItemName = "",
            ItemDescription = DEFAULT_ITEM_DESCRIPTION
        };

        // Act

        // add one item
        var addItemResult = await _sut.CreateItemAsync(commandStub);

        // Assert

        Assert.False(addItemResult.Success, "It should not be allowed to create an item without a name");
    }

    [Fact(DisplayName = "Create a new item without description")]
    public async Task CreateItem_CreateNewItemWithoutDescription_ReturnsCreatedItemWithoutDescription()
    {
        // Arrange

        string itemDescription = string.Empty;

        var commandStub = new CreateItemCommand
        {
            SenderUserId = DEFAULT_USER_ID,
            ItemName = DEFAULT_ITEM_NAME,
            ItemDescription = itemDescription
        };

        // Act

        // add one item
        var addItemResult = await _sut.CreateItemAsync(commandStub);

        // Assert

        Assert.True(addItemResult.Success, "The item creation should be successful");
        Assert.Equal(itemDescription, addItemResult.ItemDescription);
    }

    [Fact(DisplayName = "Update item")]
    public async Task UpdateItem_CreateItemAndUpdateNameAndDescription_ReturnsUpdatedItem()
    {
        // Arrange

        var createItemStub = new CreateItemCommand
        {
            SenderUserId = DEFAULT_USER_ID,
            ItemName = DEFAULT_ITEM_NAME,
            ItemDescription = DEFAULT_ITEM_DESCRIPTION
        };

        // add one item
        var addItemResult = await _sut.CreateItemAsync(createItemStub);

        string item_id = addItemResult.ItemId;

        string newItemName = DEFAULT_ITEM_NAME + "_Updated";
        string newDescription = DEFAULT_ITEM_DESCRIPTION + "_Updated";

        var commandStub = new UpdateItemCommand
        {
            ItemId = item_id,
            ItemName = newItemName,
            ItemDescription = newDescription,
            SenderUserId = DEFAULT_USER_ID
        };

        // Act

        var updateItemResult = await _sut.UpdateItemAsync(commandStub);

        // Assert

        Assert.True(updateItemResult.Success, "The item update should be successful");
        Assert.Equal(newItemName, updateItemResult.ItemName);
        Assert.Equal(newDescription, updateItemResult.ItemDescription);
    }

    [Fact(DisplayName = "Update item without creating the item first")]
    public async Task UpdateItem_UpdateItemWithoutCreatingTheItemFirst_ShouldFail()
    {
        // Arrange
        
        string newItemName = DEFAULT_ITEM_NAME + "_Updated";
        string newDescription = DEFAULT_ITEM_DESCRIPTION + "_Updated";

        var commandStub = new UpdateItemCommand
        {
            ItemId = "", // no id because there was no item created before
            ItemName = newItemName,
            ItemDescription = newDescription,
            SenderUserId = DEFAULT_USER_ID
        };

        // Act

        var updateItemResult = await _sut.UpdateItemAsync(commandStub);

        // Assert

        Assert.False(updateItemResult.Success, "The item update should be unsuccessful because no item was created first");
    }

    [Fact(DisplayName = "Delete Item")]
    public async Task DeleteItem_CreateItemThenDeleteIt_ReturnsDeleteItemResult()
    {
        // Arrange

        var createItemStub = new CreateItemCommand
        {
            SenderUserId = DEFAULT_USER_ID,
            ItemName = DEFAULT_ITEM_NAME,
            ItemDescription = DEFAULT_ITEM_DESCRIPTION
        };

        // add one item
        var addItemResult = await _sut.CreateItemAsync(createItemStub);

        string item_id = addItemResult.ItemId;

        var commandStub = new DeleteItemCommand { ItemId = item_id, UserId = DEFAULT_USER_ID };

        // Act

        var deleteItemResult = await _sut.DeleteItemAsync(commandStub);

        // Assert

        Assert.True(deleteItemResult.Success, "The item should had been deleted");
    }

    [Fact(DisplayName = "Delete Item without creating the item")]
    public async Task DeleteItem_DeleteItemWithoutCreatingTheItem_ShouldFail()
    {
        // Arrange

        string item_id = string.Empty;

        var commandStub = new DeleteItemCommand
        {
            ItemId = item_id,
            UserId = DEFAULT_USER_ID
        };

        // Act

        var deleteItemResult = await _sut.DeleteItemAsync(commandStub);

        // Assert

        Assert.False(deleteItemResult.Success, "The item should not have been deleted because it should not exist");
    }

    [Fact(DisplayName = "List items")]
    public async Task ListItems_CreateItemThenListItems_ReturnsItemIdsList()
    {
        // Arrange

        var createItemStub = new CreateItemCommand
        {
            SenderUserId = DEFAULT_USER_ID,
            ItemName = DEFAULT_ITEM_NAME,
            ItemDescription = DEFAULT_ITEM_DESCRIPTION
        };

        // add one item
        var addItemResult = await _sut.CreateItemAsync(createItemStub);

        var queryStub = new ListItemsQuery();

        // Act

        var result = await _sut.ListItemsAsync(queryStub);

        // Assert

        Assert.True(result.Success, "The response should be a success");
        Assert.True(result.ItemsId.ToList().Count > 0, "There should be at least one item");
        Assert.True(result.ItemsId.Contains(addItemResult.ItemId), "The list should contain the itemId that was received while inserting the item");
    }

    [Fact(DisplayName = "Get item")]
    public async Task GetItem_CreateItemThenGetTheItem_ReturnsItem()
    {
        // Arrange

        // add one item
        var addItemResult = await _sut.CreateItemAsync(
        new CreateItemCommand
        {
            SenderUserId = DEFAULT_USER_ID,
            ItemName = DEFAULT_ITEM_NAME,
            ItemDescription = DEFAULT_ITEM_DESCRIPTION
        });

        string item_id = addItemResult.ItemId;

        var queryStub = new GetItemQuery { ItemId = item_id };

        // Act

        var getItemResult = await _sut.GetItemAsync(queryStub);

        // Assert

        Assert.True(getItemResult.Success, "The result should be successful");
        Assert.Equal(getItemResult.ItemId, item_id);
    }

    [Fact(DisplayName = "Get item without creating it first")]
    public async Task GetItem_GetItemWithoutCreatingItFirst_ShouldFail()
    {
        // Arrange

        string item_id = string.Empty;

        var queryStub = new GetItemQuery { ItemId = item_id };

        // Act

        var getItemResult = await _sut.GetItemAsync(queryStub);

        // Assert

        Assert.False(getItemResult.Success, "The result should be unsuccessful because no item was created");
    }

    [Fact(DisplayName = "Get item name")]
    public async Task GetItemName_CreateItemThenGetItemName_ReturnsItemName()
    {
        // Arrange

        var createItemStub = new CreateItemCommand
        {
            SenderUserId = DEFAULT_USER_ID,
            ItemName = DEFAULT_ITEM_NAME,
            ItemDescription = DEFAULT_ITEM_DESCRIPTION
        };

        // add one item
        var addItemResult = await _sut.CreateItemAsync(createItemStub);

        var queryStub = new GetItemNameQuery { ItemId = addItemResult.ItemId };

        // Act

        var getItemNameResult = await _sut.GetItemNameAsync(queryStub);
        
        // Assert

        Assert.NotNull(getItemNameResult);
        Assert.Equal(getItemNameResult, DEFAULT_ITEM_NAME);
    }

    [Fact(DisplayName = "Get item description")]
    public async Task GetItemDescription_CreateItemThenGetItemDescription_ReturnsItemDescription()
    {
        // Arrange

        var createItemStub = new CreateItemCommand
        {
            SenderUserId = DEFAULT_USER_ID,
            ItemName = DEFAULT_ITEM_NAME,
            ItemDescription = DEFAULT_ITEM_DESCRIPTION
        };

        // add one item
        var addItemResult = await _sut.CreateItemAsync(createItemStub);

        var queryStub = new GetItemDescriptionQuery { ItemId = addItemResult.ItemId };

        // Act

        var getItemDescriptionResult = await _sut.GetItemDescriptionAsync(queryStub);

        // Assert

        Assert.NotNull(getItemDescriptionResult);
        Assert.Equal(getItemDescriptionResult, DEFAULT_ITEM_DESCRIPTION);
    }
}