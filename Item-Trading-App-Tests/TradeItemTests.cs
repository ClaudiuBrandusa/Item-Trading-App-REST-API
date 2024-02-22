using Item_Trading_App_REST_API.Resources.Commands.TradeItem;
using Item_Trading_App_REST_API.Data;
using Item_Trading_App_REST_API.Resources.Queries.TradeItem;
using Item_Trading_App_REST_API.Services.Cache;
using Item_Trading_App_REST_API.Services.TradeItem;
using MediatR;
using Item_Trading_App_REST_API.Resources.Queries.Item;
using Item_Trading_App_REST_API.Services.UnitOfWork;

namespace Item_Trading_App_Tests;
public class TradeItemTests
{
    private readonly ITradeItemService _sut; // service under test
    private readonly DatabaseContext _context;
    private readonly string defaultItemId = Guid.NewGuid().ToString();

    public TradeItemTests()
    {
        var databaseContextWrapper = TestingUtils.GetDatabaseContextWrapper(Guid.NewGuid().ToString());
        _context = databaseContextWrapper.ProvideDatabaseContext();
        var _mapper = TestingUtils.GetMapper();
        var cacheServiceMock = new Mock<ICacheService>();
        var mediatorMock = new Mock<IMediator>();
        var unitOfWorkMock = new Mock<IUnitOfWorkService>();

        #region MediatorMocks

        mediatorMock.Setup(x => x.Send(It.IsAny<IRequest<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IRequest<string> request, CancellationToken ct) =>
            {
                return "name";
            });

        #endregion MediatorMocks

        _sut = new TradeItemService(databaseContextWrapper, cacheServiceMock.Object, mediatorMock.Object, _mapper);
    }

    [Fact(DisplayName = "Add new trade item")]
    public async Task AddNewTradeItem()
    {
        // Arrange

        int price = 1;
        int quantity = 1;

        var commandStub = new AddTradeItemCommand
        {
            ItemId = defaultItemId,
            Name = "Item",
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
    public async Task AddNewTradeItemWithInvalidData(int price, int quantity)
    {
        // Arrange

        var commandStub = new AddTradeItemCommand
        {
            ItemId = defaultItemId,
            Name = "Item",
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
    public async Task HasTradeItem()
    {
        // Arrange

        var addTradeItemCommandStub = new AddTradeItemCommand
        {
            ItemId = defaultItemId,
            Name = "Item",
            Price = 1,
            Quantity = 1,
            TradeId = TestingData.DefaultTradeId
        };

        await _sut.AddTradeItemAsync(addTradeItemCommandStub);

        await _context.SaveChangesAsync();

        var hasTradeItemQueryStub = new HasTradeItemQuery { TradeId = TestingData.DefaultTradeId, ItemId = defaultItemId };

        // Act

        var result = await _sut.HasTradeItemAsync(hasTradeItemQueryStub);

        // Assert

        Assert.True(result, "The result value should be true");
    }

    [Fact(DisplayName = "Has trade item without adding the item first")]
    public async Task HasTradeItemWithoutAddingTheItemFirst()
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
    public async Task GetTradeItems(params string[] tradeItemIds)
    {
        // Arrange

        var tradeItemRequests = TestingData.GetTradeItemRequests(tradeItemIds);

        int length = tradeItemIds.Length;

        for (int i = 0; i < length; i++)
            await _sut.AddTradeItemAsync(tradeItemRequests[i]);

        await _context.SaveChangesAsync();

        var queryStub = new GetTradeItemsQuery { TradeId = TestingData.DefaultTradeId };

        // Act

        var result = await _sut.GetTradeItemsAsync(queryStub);

        // Assert
        
        Assert.True(result.Length == length, "The result should be successful");
    }

    [Theory(DisplayName = "Get trade item ids")]
    [InlineData("1")]
    [InlineData("1", "2", "3")]
    [InlineData("1", "2", "3", "4", "5")]
    public async Task GetItemTradeIdsAsync(params string[] tradeItemIds)
    {
        // Arrange

        var tradeItemRequests = TestingData.GetTradeItemRequests(tradeItemIds);

        for (int i = 0; i < tradeItemIds.Length; i++)
            await _sut.AddTradeItemAsync(tradeItemRequests[i]);

        await _context.SaveChangesAsync();

        var queryStub = new GetTradesUsingTheItemQuery { ItemId = tradeItemRequests[0].ItemId };

        // Act

        var result = await _sut.GetItemTradeIdsAsync(queryStub);

        // Assert

        Assert.True(result.Length == 1, "The result should be successful");
    }
}
