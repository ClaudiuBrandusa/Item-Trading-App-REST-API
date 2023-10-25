using Item_Trading_App_REST_API.Models.Item;
using Item_Trading_App_REST_API.Services.Cache;
using Item_Trading_App_REST_API.Services.Item;
using Item_Trading_App_REST_API.Services.Notification;
using Item_Trading_App_Tests.Utils;
using MediatR;
using Moq;

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
        mediatorMock.Setup(x => x.Send(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
            .Returns((IRequest request, CancellationToken ct) =>
            {
                return Task.CompletedTask;
            });

        var cacheServiceMock = new Mock<ICacheService>();

        var notificationServiceMock = new Mock<INotificationService>();

        _sut = new ItemService(TestingUtils.GetDatabaseContext(), cacheServiceMock.Object, notificationServiceMock.Object, mediatorMock.Object);
    }

    [Fact(DisplayName = "Create a new Item")]
    public async void CreateItem()
    {
        // add one item
        var addItemResult = await _sut.CreateItemAsync(
            new CreateItem
            {
                SenderUserId = userId,
                ItemName = itemName,
                ItemDescription = itemDescription
            });

        Assert.NotNull(addItemResult);
        Assert.True(addItemResult.Success, "The item creation should be successful");
    }

    [Theory(DisplayName = "Update Item")]
    [InlineData(true)]
    [InlineData(false)]
    public async void UpdateItem(bool shouldCreateTheItemFirst)
    {
        string item_id = "";

        if (shouldCreateTheItemFirst)
        {
            // add one item
            var addItemResult = await _sut.CreateItemAsync(
                new CreateItem
                {
                    SenderUserId = userId,
                    ItemName = itemName,
                    ItemDescription = itemDescription
                });
            item_id = addItemResult.ItemId;
        }

        string newItemName = itemName + "_Updated";
        string newDescription = itemDescription + "_Updated";

        var updateItemResult = await _sut.UpdateItemAsync(
            new UpdateItem
            {
                ItemId = item_id,
                ItemName = newItemName,
                ItemDescription = newDescription,
                SenderUserId = userId
            });

        Assert.NotNull(updateItemResult);
        if (shouldCreateTheItemFirst)
        {
            Assert.True(updateItemResult.Success, "The item update should be successful");
            Assert.Equal(newItemName, updateItemResult.ItemName);
            Assert.Equal(newDescription, updateItemResult.ItemDescription);
        }
        else
        {
            Assert.False(updateItemResult.Success, "The item update should be unsuccessful");
        }
    }

    [Theory(DisplayName = "Delete Item")]
    [InlineData(true)]
    [InlineData(false)]
    public async void DeleteItem(bool shouldCreateTheItemFirst)
    {
        string item_id = "";

        if (shouldCreateTheItemFirst)
        {
            // add one item
            var addItemResult = await _sut.CreateItemAsync(
                new CreateItem
                {
                    SenderUserId = userId,
                    ItemName = itemName,
                    ItemDescription = itemDescription
                });
            item_id = addItemResult.ItemId;
        }

        var deleteItemResult = await _sut.DeleteItemAsync(item_id, userId);

        Assert.NotNull(deleteItemResult);
        if (shouldCreateTheItemFirst)
            Assert.True(deleteItemResult.Success, "The item should had been deleted");
        else
            Assert.False(deleteItemResult.Success, "The item should not have been deleted because it should not exist");
    }

    [Fact(DisplayName = "List items")]
    public async void ListItems()
    {
        // add one item
        var addItemResult = await _sut.CreateItemAsync(
            new CreateItem
            {
                SenderUserId = userId,
                ItemName = itemName,
                ItemDescription = itemDescription
            });

        var result = await _sut.ListItemsAsync();

        Assert.NotNull(result);
        Assert.True(result.Success, "The response should be a success");
        Assert.True(result.ItemsId.ToList().Count > 0, "There should be at least one item");
        Assert.True(result.ItemsId.Contains(addItemResult.ItemId), "The list should contain the itemId that was received while inserting the item");
    }

    [Theory(DisplayName = "Get item")]
    [InlineData(true)]
    [InlineData(false)]
    public async void GetItem(bool shouldCreateTheItemFirst)
    {
        string item_id = "";

        if (shouldCreateTheItemFirst)
        {
            // add one item
                var addItemResult = await _sut.CreateItemAsync(
                new CreateItem
                {
                    SenderUserId = userId,
                    ItemName = itemName,
                    ItemDescription = itemDescription
                });
            item_id = addItemResult.ItemId;
        }


        var getItemResult = await _sut.GetItemAsync(item_id);

        if (shouldCreateTheItemFirst)
        {
            Assert.True(getItemResult.Success, "The result should be successful");
            Assert.Equal(getItemResult.ItemId, item_id);
        }
        else
        {
            Assert.False(getItemResult.Success, "The result should be unsuccessful");
        }
    }

    [Fact(DisplayName = "Get item name")]
    public async void GetItemName()
    {
        // add one item
        var addItemResult = await _sut.CreateItemAsync(
            new CreateItem
            {
                SenderUserId = userId,
                ItemName = itemName,
                ItemDescription = itemDescription
            });
            
        var getItemNameResult = await _sut.GetItemNameAsync(addItemResult.ItemId);
        Assert.NotNull(getItemNameResult);
        Assert.Equal(getItemNameResult, itemName);
    }

    [Fact(DisplayName = "Get item description")]
    public async void GetItemDescription()
    {
        // add one item
        var addItemResult = await _sut.CreateItemAsync(
            new CreateItem
            {
                SenderUserId = userId,
                ItemName = itemName,
                ItemDescription = itemDescription
            });

        var getItemDescriptionResult = await _sut.GetItemDescriptionAsync(addItemResult.ItemId);
        Assert.NotNull(getItemDescriptionResult);
        Assert.Equal(getItemDescriptionResult, itemDescription);
    }
}