using Item_Trading_App_REST_API.Resources.Commands.Inventory;
using Item_Trading_App_REST_API.Models.Item;
using Item_Trading_App_REST_API.Resources.Queries.Inventory;
using Item_Trading_App_REST_API.Services.Cache;
using Item_Trading_App_REST_API.Services.Inventory;
using Item_Trading_App_REST_API.Services.Notification;
using MediatR;
using Item_Trading_App_REST_API.Services.UnitOfWork;

namespace Item_Trading_App_Tests;

public class InventoryTests
{
    private readonly IInventoryService _sut; // service under test
    private readonly string userId = Guid.NewGuid().ToString();
    private readonly string itemId = Guid.NewGuid().ToString();

    public InventoryTests()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(x => x.Send(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
            .Returns((IRequest request, CancellationToken ct) =>
            {
                return Task.CompletedTask;
            });
        mediatorMock.Setup(x => x.Send(It.IsAny<IRequest<FullItemResult>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IRequest<FullItemResult> request, CancellationToken ct) =>
            {
                return new FullItemResult { ItemId = itemId, ItemName = "Item", ItemDescription = "Some description", Success = true };
            });
        mediatorMock.Setup(x => x.Send(It.IsAny<IRequest<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IRequest<string> request, CancellationToken ct) =>
            {
                return "Item";
            });

        var cacheServiceMock = new Mock<ICacheService>();
        var clientNotificationServiceMock = new Mock<IClientNotificationService>();
        var unitOfWorkMock = new Mock<IUnitOfWorkService>();

        _sut = new InventoryService(TestingUtils.GetDatabaseContextWrapper(Guid.NewGuid().ToString()), clientNotificationServiceMock.Object, cacheServiceMock.Object, mediatorMock.Object, TestingUtils.GetMapper(), unitOfWorkMock.Object);
    }

    [Theory(DisplayName = "Add item to inventory")]
    [InlineData(1)]
    [InlineData(0)]
    [InlineData(-1)]
    public async void AddItemToInventory(int quantity)
    {
        // Arrange

        var commandStub = new AddInventoryItemCommand
        {
            ItemId = itemId,
            Quantity = quantity,
            UserId = userId
        };

        // Act

        var result = await _sut.AddItemAsync(commandStub);

        // Assert

        if (quantity > 0)
        {
            Assert.True(result.Success, "The result should be successful");
            Assert.Equal(quantity, result.Quantity);
        }
        else
        {
            Assert.False(result.Success, "The result should be unsuccessful");
            Assert.Equal(0, result.Quantity);
        }
    }

    [Theory(DisplayName = "Remove item from inventory")]
    [InlineData(1, 1, 0)]
    [InlineData(5, 1, 1)]
    [InlineData(5, 1, 5)]
    [InlineData(1, 5, 0)]
    public async void RemoveItemFromInventory(int quantityToAdd, int quantityToDrop, int quantityToBeLocked)
    {
        // Arrange

        var addInventoryItemCommandStub = new AddInventoryItemCommand
        {
            ItemId = itemId,
            Quantity = quantityToAdd,
            UserId = userId
        };

        await _sut.AddItemAsync(addInventoryItemCommandStub);

        if (quantityToBeLocked > 0)
        {
            var lockItemCommandStub = new LockItemCommand
            {
                ItemId = itemId,
                Quantity = quantityToBeLocked,
                UserId = userId
            };

            await _sut.LockItemAsync(lockItemCommandStub);
        }

        var dropInventoryItemCommandStub = new DropInventoryItemCommand
        {
            ItemId = itemId,
            Quantity = quantityToDrop,
            UserId = userId
        };

        // Act

        var result = await _sut.DropItemAsync(dropInventoryItemCommandStub);

        int freeQuantity = quantityToAdd - quantityToBeLocked;

        // Assert

        Assert.True(result.Success == freeQuantity >= quantityToDrop, "You should only drop the amount that is less than or equal to the amount you have");
        if (freeQuantity >= quantityToDrop)
            Assert.Equal(freeQuantity - quantityToDrop, result.Quantity);
    }

    [Theory(DisplayName = "Has item inventory")]
    [InlineData(true, 1, 1)]
    [InlineData(true, 1, -1)]
    [InlineData(true, 2, 1)]
    [InlineData(true, 1, 2)]
    [InlineData(false, 1, 1)]
    public async void HasItemInInventory(bool addItem, int quantityToBeAdded, int quantityToBeChecked)
    {
        // Arrange

        string item_id = "";

        if (addItem)
        {
            var tmp = await _sut.AddItemAsync(new AddInventoryItemCommand
            {
                ItemId = itemId,
                Quantity = quantityToBeAdded,
                UserId = userId
            });

            item_id = tmp.ItemId;
        }

        var queryStub = new HasItemQuantityQuery
        {
            ItemId = item_id,
            Quantity = quantityToBeChecked,
            UserId = userId
        };

        // Act

        var result = await _sut.HasItemAsync(queryStub);

        // Assert

        if (addItem && quantityToBeAdded >= quantityToBeChecked && quantityToBeChecked > 0)
        {
            Assert.True(result, "The result should be true because the item should be in the inventory");
        }
        else
        {
            Assert.False(result, "The result should be false because there is no item in the inventory");
        }
    }

    [Theory(DisplayName = "Get item from inventory")]
    [InlineData(true, 1)]
    [InlineData(true, 0)]
    [InlineData(true, -1)]
    [InlineData(false, 1)]
    public async void GetItemFromInventory(bool addItem, int quantityToBeAdded)
    {
        // Arrange

        string item_id = "";

        if (addItem)
        {
            var tmp = await _sut.AddItemAsync(new AddInventoryItemCommand
            {
                ItemId = itemId,
                Quantity = quantityToBeAdded,
                UserId = userId
            });

            item_id = tmp.ItemId;
        }

        var queryStub = new GetInventoryItemQuery
        {
            UserId = userId,
            ItemId = item_id
        };

        // Act

        var result = await _sut.GetItemAsync(queryStub);

        // Assert

        if (addItem && quantityToBeAdded > 0)
        {
            Assert.True(result.Success, "The result should be successful");
            Assert.Equal(result.ItemId, itemId);
        }
        else
        {
            Assert.False(result.Success, "The result should be unsuccessful");
        }
    }

    [Theory(DisplayName = "List inventory items")]
    [InlineData(true, "1")]
    [InlineData(true, "1", "2", "3")]
    [InlineData(false)]
    public async void ListInventoryItems(bool addItems, params string[] itemIds)
    {
        // Arrange

        if (addItems)
            foreach (string item_id in itemIds)
                await _sut.AddItemAsync(new AddInventoryItemCommand
                {
                    ItemId = item_id,
                    Quantity = 1,
                    UserId = userId
                });

        var queryStub = new ListInventoryItemsQuery
        {
            SearchString = "",
            UserId = userId
        };

        // Act

        var result = await _sut.ListItemsAsync(queryStub);

        // Assert

        Assert.True(result.Success, "The result should be successful");
        Assert.True(result.ItemsId.All(x => itemIds.Contains(x)), "The result should contain all of the inserted itemIds");
    }

    [Theory(DisplayName = "Lock item")]
    [InlineData(true, 1, 1)]
    [InlineData(true, 1, 2)]
    [InlineData(true, 1, -1)]
    [InlineData(false, 1, 1)]
    public async void LockItem(bool addItem, int quantityAdded, int quantityLocked)
    {
        // Arrange

        if (addItem)
        {
            await _sut.AddItemAsync(new AddInventoryItemCommand
            {
                ItemId = itemId,
                Quantity = quantityAdded,
                UserId = userId
            });
        }

        var commandStub = new LockItemCommand
        {
            ItemId = itemId,
            Quantity = quantityLocked,
            UserId = userId
        };

        // Act

        var result = await _sut.LockItemAsync(commandStub);

        // Assert

        if (addItem && quantityLocked > 0 && quantityAdded >= quantityLocked)
        {
            Assert.True(result.Success, "The result should be successful");
            Assert.Equal(quantityLocked, result.Quantity);
            Assert.Equal(userId, result.UserId);
            Assert.Equal(itemId, result.ItemId);
        }
        else
        {
            Assert.False(result.Success, "The result should be unsuccessful");
        }
    }

    [Theory(DisplayName = "Lock and unlock item")]
    [InlineData(true, 1, 1, 1)]
    [InlineData(true, 1, 4, 2)]
    [InlineData(true, 1, 2, 1)]
    [InlineData(true, 1, -1, 1)]
    [InlineData(false, 1, 1, 1)]
    public async void UnlockItem(bool addItem, int quantityAdded, int quantityLocked, int quantityUnlocked)
    {
        // Arrange

        if (addItem)
        {
            await _sut.AddItemAsync(new AddInventoryItemCommand
            {
                ItemId = itemId,
                Quantity = quantityAdded,
                UserId = userId
            });
        }

        await _sut.LockItemAsync(new LockItemCommand
        {
            ItemId = itemId,
            Quantity = quantityLocked,
            UserId = userId
        });

        var commandStub = new UnlockItemCommand
        {
            ItemId = itemId,
            Quantity = quantityUnlocked,
            UserId = userId
        };

        // Act

        var result = await _sut.UnlockItemAsync(commandStub);

        // Assert

        if (addItem && quantityLocked > 0 && quantityUnlocked > 0 && quantityAdded >= quantityLocked && quantityUnlocked >= quantityLocked)
        {
            Assert.True(result.Success, "The result should be successful");
            Assert.Equal(quantityLocked - quantityUnlocked, result.Quantity);
            Assert.Equal(userId, result.UserId);
            Assert.Equal(itemId, result.ItemId);
        }
        else
        {
            Assert.False(result.Success, "The result should be unsuccessful");
        }
    }

    [Theory(DisplayName = "Get locked amount")]
    [InlineData(true, 6, 2)]
    [InlineData(true, 4, 4)]
    [InlineData(true, 3, 6)]
    [InlineData(false, 2, 1)]
    [InlineData(false, 4, 2)]
    public async void GetLockedAmount(bool addItem, int quantityAdded, int quantityLocked)
    {
        // Arrange

        if (addItem)
        {
            await _sut.AddItemAsync(new AddInventoryItemCommand
            {
                ItemId = itemId,
                Quantity = quantityAdded,
                UserId = userId
            });

            await _sut.LockItemAsync(new LockItemCommand
            {
                ItemId = itemId,
                Quantity = quantityLocked,
                UserId = userId
            });
        }

        var queryStub = new GetInventoryItemLockedAmountQuery
        {
            UserId = userId,
            ItemId = itemId
        };

        // Act

        var result = await _sut.GetLockedAmountAsync(queryStub);

        // Assert

        Assert.True(result.Success, "The result should be successful");

        if (addItem && quantityAdded >= quantityLocked)
        {
            Assert.Equal(quantityLocked, result.Amount);
        }
        else
        {
            Assert.Equal(0, result.Amount);
        }
        if (addItem)
        {
            Assert.Equal(itemId, result.ItemId);
        }
    }

    [Theory(DisplayName = "List users that own the item")]
    [InlineData("1", "2", "3")]
    [InlineData("1", "2")]
    public async void ListUsersThatOwnTheItem(params string[] userIds)
    {
        // Arrange

        foreach(string userId in userIds)
        {
            await _sut.AddItemAsync(new AddInventoryItemCommand
            {
                ItemId = itemId,
                UserId = userId,
                Quantity = 1
            });
        }

        var queryStub = new GetUserIdsOwningItemQuery { ItemId = itemId };

        // Act

        var result = await _sut.GetUsersOwningThisItemAsync(queryStub);

        // Assert

        Assert.True(result.UserIds.All(x => userIds.Contains(x)));
    }
}
