using Item_Trading_App_REST_API.Models.Inventory;
using Item_Trading_App_REST_API.Resources.Commands.Item;
using Item_Trading_App_REST_API.Resources.Queries.Inventory;
using Item_Trading_App_REST_API.Resources.Queries.Item;
using Item_Trading_App_REST_API.Services.Cache;
using Item_Trading_App_REST_API.Services.Item;
using MediatR;

namespace Item_Trading_App_Tests;

public class ItemTests
{
    private readonly IItemService _sut; // service under test
    private readonly string userId = Guid.NewGuid().ToString();
    private readonly string itemName = "Item_Name";
    private readonly string itemDescription = "Item_Description";

    public ItemTests()
    {
        var mediatorMock = new Mock<IMediator>();
        var cacheServiceMock = new Mock<ICacheService>();

        #region MediatorMocks

        mediatorMock.Setup(x => x.Send(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
            .Returns((IRequest request, CancellationToken ct) =>
            {
                return Task.CompletedTask;
            });

        mediatorMock.Setup(x => x.Send(It.IsAny<IRequest<UsersOwningItem>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IRequest<UsersOwningItem> request, CancellationToken ct) =>
            {
                return new UsersOwningItem { ItemId = ((GetUserIdsOwningItemQuery)request).ItemId};
            });

        #endregion MediatorMocks

        _sut = new ItemService(TestingUtils.GetDatabaseContext(), cacheServiceMock.Object, mediatorMock.Object, TestingUtils.GetMapper());
    }

    [Fact(DisplayName = "Create a new Item")]
    public async Task CreateItem()
    {
        // Arrange

        var commandStub = new CreateItemCommand
        {
            SenderUserId = userId,
            ItemName = itemName,
            ItemDescription = itemDescription
        };

        // Act

        // add one item
        var addItemResult = await _sut.CreateItemAsync(commandStub);

        // Assert

        Assert.True(addItemResult.Success, "The item creation should be successful");
    }

    [Fact(DisplayName = "Update Item")]
    public async Task UpdateItem()
    {
        // Arrange

        // add one item
        var addItemResult = await _sut.CreateItemAsync(
            new CreateItemCommand
            {
                SenderUserId = userId,
                ItemName = itemName,
                ItemDescription = itemDescription
            });

        string item_id = addItemResult.ItemId;

        string newItemName = itemName + "_Updated";
        string newDescription = itemDescription + "_Updated";

        var commandStub = new UpdateItemCommand
        {
            ItemId = item_id,
            ItemName = newItemName,
            ItemDescription = newDescription,
            SenderUserId = userId
        };

        // Act

        var updateItemResult = await _sut.UpdateItemAsync(commandStub);

        // Assert

        Assert.True(updateItemResult.Success, "The item update should be successful");
        Assert.Equal(newItemName, updateItemResult.ItemName);
        Assert.Equal(newDescription, updateItemResult.ItemDescription);
    }

    [Fact(DisplayName = "Update item without creating the item first")]
    public async Task UpdateItemWithoutCreatingTheItemFirst()
    {
        // Arrange
        
        string newItemName = itemName + "_Updated";
        string newDescription = itemDescription + "_Updated";

        var commandStub = new UpdateItemCommand
        {
            ItemId = "", // no id because there was no item created before
            ItemName = newItemName,
            ItemDescription = newDescription,
            SenderUserId = userId
        };

        // Act

        var updateItemResult = await _sut.UpdateItemAsync(commandStub);

        // Assert

        Assert.False(updateItemResult.Success, "The item update should be unsuccessful because no item was created first");
    }

    [Fact(DisplayName = "Delete Item")]
    public async Task DeleteItem()
    {
        // Arrange

        var createItemStub = new CreateItemCommand
        {
            SenderUserId = userId,
            ItemName = itemName,
            ItemDescription = itemDescription
        };

        // add one item
        var addItemResult = await _sut.CreateItemAsync(createItemStub);

        string item_id = addItemResult.ItemId;

        var commandStub = new DeleteItemCommand { ItemId = item_id, UserId = userId };

        // Act

        var deleteItemResult = await _sut.DeleteItemAsync(commandStub);

        // Assert

        Assert.True(deleteItemResult.Success, "The item should had been deleted");
    }

    [Fact(DisplayName = "Delete Item without creating the item")]
    public async Task DeleteItemWithoutCreatingTheItem()
    {
        // Arrange

        string item_id = "";

        var commandStub = new DeleteItemCommand
        {
            ItemId = item_id,
            UserId = userId
        };

        // Act

        var deleteItemResult = await _sut.DeleteItemAsync(commandStub);

        // Assert

        Assert.False(deleteItemResult.Success, "The item should not have been deleted because it should not exist");
    }

    [Fact(DisplayName = "List items")]
    public async Task ListItems()
    {
        // Arrange

        var createItemStub = new CreateItemCommand
        {
            SenderUserId = userId,
            ItemName = itemName,
            ItemDescription = itemDescription
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
    public async Task GetItem()
    {
        // Arrange

        // add one item
            var addItemResult = await _sut.CreateItemAsync(
            new CreateItemCommand
            {
                SenderUserId = userId,
                ItemName = itemName,
                ItemDescription = itemDescription
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
    public async Task GetItemWithoutCreatingItFirst()
    {
        // Arrange

        string item_id = "";

        var queryStub = new GetItemQuery { ItemId = item_id };

        // Act

        var getItemResult = await _sut.GetItemAsync(queryStub);

        // Assert

        Assert.False(getItemResult.Success, "The result should be unsuccessful because no item was created");
    }

    [Fact(DisplayName = "Get item name")]
    public async Task GetItemName()
    {
        // Arrange

        var createItemStub = new CreateItemCommand
        {
            SenderUserId = userId,
            ItemName = itemName,
            ItemDescription = itemDescription
        };

        // add one item
        var addItemResult = await _sut.CreateItemAsync(createItemStub);

        var queryStub = new GetItemNameQuery { ItemId = addItemResult.ItemId };

        // Act

        var getItemNameResult = await _sut.GetItemNameAsync(queryStub);
        
        // Assert

        Assert.NotNull(getItemNameResult);
        Assert.Equal(getItemNameResult, itemName);
    }

    [Fact(DisplayName = "Get item description")]
    public async Task GetItemDescription()
    {
        // Arrange

        var createItemStub = new CreateItemCommand
        {
            SenderUserId = userId,
            ItemName = itemName,
            ItemDescription = itemDescription
        };

        // add one item
        var addItemResult = await _sut.CreateItemAsync(createItemStub);

        var queryStub = new GetItemDescriptionQuery { ItemId = addItemResult.ItemId };

        // Act

        var getItemDescriptionResult = await _sut.GetItemDescriptionAsync(queryStub);

        // Assert

        Assert.NotNull(getItemDescriptionResult);
        Assert.Equal(getItemDescriptionResult, itemDescription);
    }
}