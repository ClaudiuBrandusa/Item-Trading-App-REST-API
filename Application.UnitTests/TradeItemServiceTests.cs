using MediatR;
using Application.Services.UnitOfWork;
using Application.Behaviors.TradeItem.AddTradeItem;
using Application.Behaviors.TradeItem.GetTradeItemIds;
using Application.Behaviors.TradeItem.HasTradeItem;
using Application.Behaviors.TradeItem.GetTradeItems;
using Domain.Entities.Trades;
using Application.Services.TradeItems;
using Application.Repositories;

namespace Application_UnitTests;
public class TradeItemServiceTests
{
    private readonly ITradeItemService _sut; // service under test
    private readonly string defaultItemId = Guid.NewGuid().ToString();
    private readonly List<TradeItem> collection;

    public TradeItemServiceTests()
    {
        collection = new List<TradeItem>();
        var tradeItemRepositoryMock = TestingUtils.CreateRepositoryMock<TradeItem, ICachedTradeItemRepository> (collection);
        var _mapper = TestingUtils.GetMapper();
        var mediatorMock = new Mock<IMediator>();
        var unitOfWorkMock = new Mock<IUnitOfWorkService>();

        #region MediatorMocks

        mediatorMock.Setup(x => x.Send(It.IsAny<IRequest<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IRequest<string> request, CancellationToken ct) =>
            {
                return "name";
            });

        tradeItemRepositoryMock.Setup(repo => repo.GetTradeItemAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string tradeId, string itemId) =>
            {
                return GetTradeContent(tradeId, itemId);
            });

        tradeItemRepositoryMock.Setup(repo => repo.GetTradeIdsUsingItemAsync(It.IsAny<string>()))
            .ReturnsAsync((string itemId) =>
            {
                return GetTradeIdsUsingItemId(itemId);
            });

        tradeItemRepositoryMock.Setup(repo => repo.ListTradeItemsAsync(It.IsAny<string>()))
            .ReturnsAsync((string tradeId) =>
            {
                return collection.Where(x => x.TradeId == tradeId)
                                 .ToArray();
            });

        #endregion MediatorMocks

        _sut = new TradeItemService(tradeItemRepositoryMock.Object, mediatorMock.Object, _mapper);
    }

    [Fact(DisplayName = "Add new trade item")]
    public async Task AddTradeItem_AddNewTradeItem_ReturnsTrue()
    {
        // Arrange

        int price = 1;
        int quantity = 1;

        var commandStub = new AddTradeItemCommand
        {
            ItemId = defaultItemId,
            Price = price,
            Quantity = quantity,
            TradeId = TestingData.DefaultTradeId
        };

        // Act

        var result = await _sut.AddTradeItemAsync(commandStub);

        // Assert

        Assert.True(result, "The result should be successful");
    }

    [Theory(DisplayName = "Add new trade item with invalid data")]
    [InlineData(1, 0)]
    [InlineData(0, 0)]
    [InlineData(1, -2)]
    [InlineData(-1, 1)]
    public async Task AddTradeItem_AddNewTradeItemWithInvalidData_ReturnsFalse(int price, int quantity)
    {
        // Arrange

        var commandStub = new AddTradeItemCommand
        {
            ItemId = defaultItemId,
            Price = price,
            Quantity = quantity,
            TradeId = TestingData.DefaultTradeId
        };

        // Act

        var result = await _sut.AddTradeItemAsync(commandStub);

        // Assert

        Assert.False(result, "The result should be unsuccessful because the input data was invalid");
    }

    [Fact(DisplayName = "Has trade item")]
    public async Task HasTradeItem_AddTradeItemThenCheckIfItHasTradeItem_ReturnsTrue()
    {
        // Arrange

        var addTradeItemCommandStub = new AddTradeItemCommand
        {
            ItemId = defaultItemId,
            Price = 1,
            Quantity = 1,
            TradeId = TestingData.DefaultTradeId
        };

        await _sut.AddTradeItemAsync(addTradeItemCommandStub);

        var hasTradeItemQueryStub = new HasTradeItemQuery { TradeId = TestingData.DefaultTradeId, ItemId = defaultItemId };

        // Act

        var result = await _sut.HasTradeItemAsync(hasTradeItemQueryStub);

        // Assert

        Assert.True(result, "The result value should be true");
    }

    [Fact(DisplayName = "Has trade item without adding the item first")]
    public async Task HasTradeItem_CheckIfItHasTradeItemWithoutAddingTheItemFirst_ReturnsFalse()
    {
        // Arrange

        var hasTradeItemQueryStub = new HasTradeItemQuery { TradeId = TestingData.DefaultTradeId, ItemId = defaultItemId };

        // Act

        var result = await _sut.HasTradeItemAsync(hasTradeItemQueryStub);

        // Assert

        Assert.False(result, "The result value should be false because no trade item was added first");
    }

    [Theory(DisplayName = "Get trade items")]
    [InlineData("1")]
    [InlineData("1", "2", "3")]
    [InlineData("1", "2", "3", "4", "5")]
    public async Task GetTradeItems_AddSeveralTradeItemsThenGetTradeItems_ReturnsTradeItemsArray(params string[] tradeItemIds)
    {
        // Arrange

        var tradeItemRequests = TestingData.GetTradeItemRequests(tradeItemIds);

        int length = tradeItemIds.Length;

        for (int i = 0; i < length; i++)
            await _sut.AddTradeItemAsync(tradeItemRequests[i]);

        var queryStub = new GetTradeItemsQuery { TradeId = TestingData.DefaultTradeId };

        // Act

        var result = await _sut.GetTradeItemAsync(queryStub);

        // Assert
        
        Assert.True(result.Length == length, "The result should be successful");
    }

    [Theory(DisplayName = "Get trade item ids")]
    [InlineData("1")]
    [InlineData("1", "2", "3")]
    [InlineData("1", "2", "3", "4", "5")]
    public async Task GetItemTradeIds_AddSeveralItemTradesThenGetItemTradeIds_ReturnsItemTradeIdsArray(params string[] tradeItemIds)
    {
        // Arrange

        var tradeItemRequests = TestingData.GetTradeItemRequests(tradeItemIds);

        for (int i = 0; i < tradeItemIds.Length; i++)
            await _sut.AddTradeItemAsync(tradeItemRequests[i]);

        var queryStub = new GetTradesUsingTheItemQuery { ItemId = tradeItemRequests[0].ItemId };

        // Act

        var result = await _sut.GetItemTradeIdsAsync(queryStub);

        // Assert

        Assert.True(result.Length == 1, "The result should be successful");
    }

    #region Utils

    private TradeItem? GetTradeContent(string tradeId, string itemId)
    {
        return collection.FirstOrDefault(x => x.TradeId == tradeId && x.ItemId == itemId);
    }

    private string[] GetTradeIdsUsingItemId(string itemId)
    {
        return collection.Where(x => x.ItemId == itemId).Select(x => x.TradeId).ToArray();
    }

    #endregion Utils
}
