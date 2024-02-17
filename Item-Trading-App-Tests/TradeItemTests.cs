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

        mediatorMock.Setup(x => x.Send(It.IsAny<IRequest<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IRequest<string> request, CancellationToken ct) =>
            {
                return "name";
            });

        var unitOfWorkMock = new Mock<IUnitOfWorkService>();

        _sut = new TradeItemService(databaseContextWrapper, cacheServiceMock.Object, mediatorMock.Object, _mapper, unitOfWorkMock.Object);
    }

    [Theory(DisplayName = "Add new trade item")]
    [InlineData(1, 1)]
    [InlineData(1, 0)]
    [InlineData(0, 0)]
    [InlineData(1, -2)]
    [InlineData(-1, 1)]
    public async void AddNewTradeItem(int price, int quantity)
    {
        var result = await _sut.AddTradeItemAsync(new AddTradeItemCommand
        {
            ItemId = defaultItemId,
            Name = "Item",
            Price = price,
            Quantity = quantity,
            TradeId = TestingData.DefaultTradeId
        });

        if (price > 0 && quantity > 0)
        {
            Assert.True(result, "The result should be successful");
        } else
        {
            Assert.False(result, "The result should be unsuccessful");
        }
    }

    [Fact(DisplayName = "Has trade item")]
    public async void HasTradeItem()
    {
        await _sut.AddTradeItemAsync(new AddTradeItemCommand
        {
            ItemId = defaultItemId,
            Name = "Item",
            Price = 1,
            Quantity = 1,
            TradeId = TestingData.DefaultTradeId
        });

        await _context.SaveChangesAsync();

        var result = await _sut.HasTradeItemAsync(new HasTradeItemQuery { TradeId = TestingData.DefaultTradeId, ItemId = defaultItemId });

        Assert.True(result, "The result value should be true");
    }

    [Theory(DisplayName = "Get item prices")]
    [InlineData("1")]
    [InlineData("1", "2", "3")]
    [InlineData("1", "2", "3", "4", "5")]
    public async void GetTradeItems(params string[] tradeItemIds)
    {
        var tradeItemRequests = TestingData.GetTradeItemRequests(tradeItemIds);

        int length = tradeItemIds.Length;

        for (int i = 0; i < length; i++)
            await _sut.AddTradeItemAsync(tradeItemRequests[i]);

        await _context.SaveChangesAsync();

        var result = await _sut.GetTradeItemsAsync(new GetTradeItemsQuery { TradeId = TestingData.DefaultTradeId });

        Assert.True(result.Length == length, "The result should be successful");
    }

    [Theory(DisplayName = "Get item prices")]
    [InlineData("1")]
    [InlineData("1", "2", "3")]
    [InlineData("1", "2", "3", "4", "5")]
    public async void GetItemTradeIdsAsync(params string[] tradeItemIds)
    {
        var tradeItemRequests = TestingData.GetTradeItemRequests(tradeItemIds);

        for (int i = 0; i < tradeItemIds.Length; i++)
            await _sut.AddTradeItemAsync(tradeItemRequests[i]);

        await _context.SaveChangesAsync();

        var result = await _sut.GetItemTradeIdsAsync(new GetTradesUsingTheItemQuery { ItemId = tradeItemRequests[0].ItemId });

        Assert.True(result.Length == 1, "The result should be successful");
    }
}
