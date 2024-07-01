using Domain.Repositories;
using Domain.Trades;
using Infrastructure.Repositories;
using Infrastructure.Services.DatabaseContextWrapper;
using Infrastructure_IntegrationTests.Utils;

namespace Infrastructure_UnitTests;

public class TradeItemRepositoryTests
{
    private readonly ITradeItemRepository _sut;

    private IDatabaseContextWrapper _contextWrapper;

    public TradeItemRepositoryTests()
    {
        _contextWrapper = TestingUtils.GetDatabaseContextWrapper(Guid.NewGuid().ToString());
        var cacheServiceMock = TestingUtils.GetCacheServiceMock();
        var mapper = TestingUtils.GetMapper();

        _sut = new TradeItemRepository(_contextWrapper, cacheServiceMock.Object, mapper);
    }

    [Fact(DisplayName = "Add trade content and get the added trade content")]
    public async Task GetTradeContent_AddTradeContentAndGetTheTradeContent_ReturnsTheAddedContent()
    {
        // Arrange

        string tradeId = Guid.NewGuid().ToString();
        string itemId = Guid.NewGuid().ToString();
        int price = 5;
        int quantity = 2;

        var tradeContentMock = new TradeContent
        {
            TradeId = tradeId,
            ItemId = itemId,
            Price = price,
            Quantity = quantity
        };

        var addTradeContentResult = await _sut.AddEntityAsync(tradeContentMock);

        // Act

        var tradeContentResult = await _sut.GetTradeContentAsync(tradeId, itemId);

        // Assert

        Assert.NotNull(tradeContentResult);
        Assert.Equal(tradeId, tradeContentResult.TradeId);
        Assert.Equal(itemId, tradeContentResult.ItemId);
        Assert.Equal(price, tradeContentResult.Price);
        Assert.Equal(quantity, tradeContentResult.Quantity);
    }

    [Fact(DisplayName = "Add trade content and get the added trade content (cached)")]
    public async Task GetTradeContentCached_AddTradeContentAndGetTheTradeContent_ReturnsTheCachedAddedContent()
    {
        // Arrange

        string tradeId = Guid.NewGuid().ToString();
        string itemId = Guid.NewGuid().ToString();
        int price = 5;
        int quantity = 2;

        var tradeContentMock = new TradeContent
        {
            TradeId = tradeId,
            ItemId = itemId,
            Price = price,
            Quantity = quantity
        };

        var addTradeContentResult = await _sut.AddEntityAsync(tradeContentMock);

        // Act

        var tradeContentResult = await _sut.GetTradeContentCachedAsync(tradeId, itemId);

        // Assert

        Assert.NotNull(tradeContentResult);
        Assert.Equal(tradeId, tradeContentResult.TradeId);
        Assert.Equal(itemId, tradeContentResult.ItemId);
        Assert.Equal(price, tradeContentResult.Price);
        Assert.Equal(quantity, tradeContentResult.Quantity);
    }

    [Fact(DisplayName = "Add several trade contents and get a list with trade's trade contents")]
    public async Task ListTradeContents_AddSeveralTradeContentsAndGetListWithTradeContents_ReturnsTradesTradeContentsList()
    {
        // Arrange

        int count = 5;
        string tradeId = Guid.NewGuid().ToString();

        for (int i = 0; i < count; i++)
        {
            string itemId = Guid.NewGuid().ToString();
            int price = 5;
            int quantity = 2;

            var tradeContentMock = new TradeContent
            {
                TradeId = tradeId,
                ItemId = itemId,
                Price = price,
                Quantity = quantity
            };

            var addTradeContentResult = await _sut.AddEntityAsync(tradeContentMock);
        }

        // Act

        var tradeContentsResult = await _sut.ListTradeContentsAsync(tradeId);

        // Assert

        Assert.NotNull(tradeContentsResult);
        Assert.Equal(count, tradeContentsResult.Length);
    }

    [Fact(DisplayName = "Add several trade contents and get a list with trade's trade contents (cached)")]
    public async Task ListTradeItemsCached_AddSeveralTradeContentsAndGetListWithTradeItems_ReturnsCachedTradesTradeItemsList()
    {
        // Arrange

        int count = 5;
        string tradeId = Guid.NewGuid().ToString();

        for (int i = 0; i < count; i++)
        {
            string itemId = Guid.NewGuid().ToString();
            int price = 5;
            int quantity = 2;

            var tradeContentMock = new TradeContent
            {
                TradeId = tradeId,
                ItemId = itemId,
                Price = price,
                Quantity = quantity
            };

            var addTradeContentResult = await _sut.AddEntityAsync(tradeContentMock);
        }

        // Act

        var tradeItemsResult = await _sut.ListTradeItemsCachedAsync(tradeId, (string input) => Task.FromResult("DefaultName"));

        // Assert

        Assert.NotNull(tradeItemsResult);
        Assert.Equal(count, tradeItemsResult.Length);
    }

    [Fact(DisplayName = "Add several trade contents to different trades and get a list with trades that own the item")]
    public async Task GetTradeIdsUsingItem_AddSeveralTradeContentsToDifferentTradesAndGetListWithTradesThatOwnTheItem_ReturnsTradeIdsThatOwnTheItem()
    {
        // Arrange

        int count = 5;
        string itemId = Guid.NewGuid().ToString();

        for (int i = 0; i < count; i++)
        {
            int price = 5;
            int quantity = 2;

            var tradeContentMock = new TradeContent
            {
                TradeId = Guid.NewGuid().ToString(),
                ItemId = itemId,
                Price = price,
                Quantity = quantity
            };

            var addTradeContentResult = await _sut.AddEntityAsync(tradeContentMock);
        }

        // Act

        var tradeContentResult = await _sut.GetTradeIdsUsingItemAsync(itemId);

        // Assert

        Assert.NotNull(tradeContentResult);
        Assert.Equal(count, tradeContentResult.Length);
    }

    [Fact(DisplayName = "Add several trade contents to different trades and get a list with trades that own the item (cached)")]
    public async Task GetTradeIdsUsingItem_AddSeveralTradeContentsToDifferentTradesAndGetListWithTradesThatOwnTheItem_ReturnsCachedTradeIdsThatOwnTheItem()
    {
        // Arrange

        int count = 5;
        string itemId = Guid.NewGuid().ToString();

        for (int i = 0; i < count; i++)
        {
            int price = 5;
            int quantity = 2;

            var tradeContentMock = new TradeContent
            {
                TradeId = Guid.NewGuid().ToString(),
                ItemId = itemId,
                Price = price,
                Quantity = quantity
            };

            var addTradeContentResult = await _sut.AddEntityAsync(tradeContentMock);
        }

        // Act

        var tradeContentResult = await _sut.GetTradeIdsUsingItemCachedAsync(itemId);

        // Assert

        Assert.NotNull(tradeContentResult);
        Assert.Equal(count, tradeContentResult.Length);
    }

    [Fact(DisplayName = "Add several trade contents and get a list with trade's trade contents")]
    public async Task DeleteTradeItems_AddSeveralTradeContentsAndDeleteAllTradeContents_ReturnsTrue()
    {
        // Arrange

        int count = 5;
        string tradeId = Guid.NewGuid().ToString();

        for (int i = 0; i < count; i++)
        {
            string itemId = Guid.NewGuid().ToString();
            int price = 5;
            int quantity = 2;

            var tradeContentMock = new TradeContent
            {
                TradeId = tradeId,
                ItemId = itemId,
                Price = price,
                Quantity = quantity
            };

            var addTradeContentResult = await _sut.AddEntityAsync(tradeContentMock);
        }

        await _contextWrapper.ProvideDatabaseContext().SaveChangesAsync();

        // Act

        var deleteTradeContentsResult = await _sut.DeleteTradeItemsAsync(tradeId);

        // Assert

        Assert.True(deleteTradeContentsResult);
    }
}
