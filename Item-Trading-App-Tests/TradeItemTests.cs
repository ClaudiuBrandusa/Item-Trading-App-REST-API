using Item_Trading_App_REST_API.Resources.Commands.TradeItem;
using Item_Trading_App_REST_API.Data;
using Item_Trading_App_REST_API.Entities;
using Item_Trading_App_REST_API.Resources.Queries.Trade;
using Item_Trading_App_REST_API.Resources.Queries.TradeItem;
using Item_Trading_App_REST_API.Services.Cache;
using Item_Trading_App_REST_API.Services.TradeItem;
using MediatR;

namespace Item_Trading_App_Tests;
public class TradeItemTests
{
    private readonly ITradeItemService _sut; // service under test
    private readonly DatabaseContext _context;
    private readonly string defaultItemId = Guid.NewGuid().ToString();

    public TradeItemTests()
    {
        _context = TestingUtils.GetDatabaseContext();
        var _mapper = TestingUtils.GetMapper();

        var cacheServiceMock = new Mock<ICacheService>();

        var mediatorMock = new Mock<IMediator>();

        mediatorMock.Setup(x => x.Send(It.IsAny<IRequest<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IRequest<string> request, CancellationToken ct) =>
            {
                return "name";
            });

        _sut = new TradeItemService(_context, cacheServiceMock.Object, mediatorMock.Object, _mapper);
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

    [Theory(DisplayName = "Get trade items")]
    [InlineData("1")]
    [InlineData("1", "2", "3")]
    [InlineData("1", "2", "3", "4", "5")]
    public async void GetTradeItems(params string[] itemPriceIds)
    {
        var tradeItemRequests = TestingData.GetTradeItemRequests(itemPriceIds);

        // _tradeService.CreateTradeOffer()

        var offer = new Trade
        {
            TradeId = tradeItemRequests[0].TradeId,
            SentDate = DateTime.Now
        };

        await _context.AddEntityAsync(offer);

        int length = itemPriceIds.Length;

        for (int i = 0; i < length; i++)
        {
            await _sut.AddTradeItemAsync(tradeItemRequests[i]);
        }

        await _context.SaveChangesAsync();
        var result = await _sut.GetTradeItemsAsync(new GetTradeItemsQuery { TradeId = TestingData.DefaultTradeId });

        Assert.True(result.Count == length, "The result should be successful");
    }

    [Theory(DisplayName = "Get item prices")]
    [InlineData("1")]
    [InlineData("1", "2", "3")]
    [InlineData("1", "2", "3", "4", "5")]
    public async void GetItemPrices(params string[] itemPriceIds)
    {
        var tradeItemRequests = TestingData.GetTradeItemRequests(itemPriceIds);

        int length = itemPriceIds.Length;

        for (int i = 0; i < length; i++)
            await _sut.AddTradeItemAsync(tradeItemRequests[i]);

        await _context.SaveChangesAsync();

        var result = await _sut.GetItemPricesAsync(new GetItemPricesQuery { TradeId = TestingData.DefaultTradeId });

        Assert.True(result.Count == length, "The result should be successful");
    }

    [Theory(DisplayName = "Get item prices")]
    [InlineData("1")]
    [InlineData("1", "2", "3")]
    [InlineData("1", "2", "3", "4", "5")]
    public async void GetItemTradeIdsAsync(params string[] itemPriceIds)
    {
        var tradeItemRequests = TestingData.GetTradeItemRequests(itemPriceIds);

        for (int i = 0; i < itemPriceIds.Length; i++)
            await _sut.AddTradeItemAsync(tradeItemRequests[i]);

        await _context.SaveChangesAsync();

        var result = await _sut.GetItemTradeIdsAsync(new ItemUsedInTradeQuery { ItemId = tradeItemRequests[0].ItemId });

        Assert.True(result.Count == 1, "The result should be successful");
    }
}
