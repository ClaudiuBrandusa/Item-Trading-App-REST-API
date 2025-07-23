using Domain.Entities.Trades;
using Domain.Entities.Items;
using Infrastructure.Services.DatabaseContextWrapper;
using Infrastructure_IntegrationTests.Utils;
using Domain.Aggregates.Trades;
using Domain.Repositories.TradeItems;
using Infrastructure.Repositories.TradeItems;

namespace Infrastructure_UnitTests.RepositoryTests;

public class TradeItemRepositoryTests
{
    private readonly ITradeItemRepository _sut;

    private readonly IDatabaseContextWrapper _contextWrapper;

    public TradeItemRepositoryTests()
    {
        _contextWrapper = TestingUtils.GetDatabaseContextWrapper(Guid.NewGuid().ToString());
        
        _sut = new TradeContentRepository(_contextWrapper);
    }

    [Fact(DisplayName = "Add trade content and get the added trade content")]
    public async Task GetTradeItem_AddTradeContentAndGetTheTradeContent_ReturnsTheAddedContent()
    {
        // Arrange

        string tradeId = Trade.GenerateId();
        string itemId = Item.GenerateId();
        int quantity = 2;
        int price = 5;

        var tradeContentMock = new TradeItem(tradeId, itemId, quantity, price);

        await _sut.AddEntityAsync(tradeContentMock);

        // Act

        var tradeContentResult = await _sut.GetTradeItemAsync(tradeId, itemId);

        // Assert

        Assert.NotNull(tradeContentResult);
        Assert.Equal(tradeContentMock, tradeContentResult);
    }

    /*[Fact(DisplayName = "Add trade content and get the added trade content (cached)")]
    public async Task GetTradeContentCached_AddTradeContentAndGetTheTradeContent_ReturnsTheCachedAddedContent()
    {
        // Arrange

        string tradeId = Trade.GenerateId();
        string itemId = Item.GenerateId();
        int price = 5;
        int quantity = 2;

        var tradeContentMock = new TradeContent(tradeId, itemId, quantity, price);

        await _sut.AddEntityAsync(tradeContentMock);

        // Act

        var tradeContentResult = await _sut.GetTradeContentCachedAsync(tradeId, itemId);

        // Assert

        Assert.NotNull(tradeContentResult);
        Assert.Equal(tradeContentMock, tradeContentResult);
    }*/

    [Fact(DisplayName = "Add several trade contents and get a list with trade's trade contents")]
    public async Task ListTradeItems_AddSeveralTradeContentsAndGetListWithTradeContents_ReturnsTradesTradeContentsList()
    {
        // Arrange

        int count = 5;
        string tradeId = Trade.GenerateId();

        for (int i = 0; i < count; i++)
        {
            string itemId = Item.GenerateId();
            int price = 5;
            int quantity = 2;

            var tradeContentMock = new TradeItem(tradeId, itemId, quantity, price);

            await _sut.AddEntityAsync(tradeContentMock);
        }

        // Act

        var tradeContentsResult = await _sut.ListTradeItemsAsync(tradeId);

        // Assert

        Assert.NotNull(tradeContentsResult);
        Assert.Equal(count, tradeContentsResult.Length);
    }

    /*[Fact(DisplayName = "Add several trade contents and get a list with trade's trade contents (cached)")]
    public async Task ListTradeItemsCached_AddSeveralTradeContentsAndGetListWithTradeItems_ReturnsCachedTradesTradeItemsList()
    {
        // Arrange

        int count = 5;
        string tradeId = Trade.GenerateId();

        for (int i = 0; i < count; i++)
        {
            string itemId = Item.GenerateId();
            int price = 5;
            int quantity = 2;

            var tradeContentMock = new TradeContent(tradeId, itemId, quantity, price);

            await _sut.AddEntityAsync(tradeContentMock);
        }

        // Act

        var tradeItemsResult = await _sut.ListTradeItemsCachedAsync(tradeId, (string input) => Task.FromResult("DefaultName"));

        // Assert

        Assert.NotNull(tradeItemsResult);
        Assert.Equal(count, tradeItemsResult.Length);
    }*/

    [Fact(DisplayName = "Add several trade contents to different trades and get a list with trades that own the item")]
    public async Task GetTradeIdsUsingItem_AddSeveralTradeContentsToDifferentTradesAndGetListWithTradesThatOwnTheItem_ReturnsTradeIdsThatOwnTheItem()
    {
        // Arrange

        int count = 5;
        string itemId = Item.GenerateId();

        for (int i = 0; i < count; i++)
        {
            int price = 5;
            int quantity = 2;

            var tradeContentMock = new TradeItem(Trade.GenerateId(), itemId, quantity, price);

            await _sut.AddEntityAsync(tradeContentMock);
        }

        // Act

        var tradeContentResult = await _sut.GetTradeIdsUsingItemAsync(itemId);

        // Assert

        Assert.NotNull(tradeContentResult);
        Assert.Equal(count, tradeContentResult.Length);
    }

    /*[Fact(DisplayName = "Add several trade contents to different trades and get a list with trades that own the item (cached)")]
    public async Task GetTradeIdsUsingItem_AddSeveralTradeContentsToDifferentTradesAndGetListWithTradesThatOwnTheItem_ReturnsCachedTradeIdsThatOwnTheItem()
    {
        // Arrange

        int count = 5;
        string itemId = Item.GenerateId();

        for (int i = 0; i < count; i++)
        {
            int price = 5;
            int quantity = 2;

            var tradeContentMock = new TradeContent(Trade.GenerateId(), itemId, quantity, price);

            await _sut.AddEntityAsync(tradeContentMock);
        }

        // Act

        var tradeContentResult = await _sut.GetTradeIdsUsingItemCachedAsync(itemId);

        // Assert

        Assert.NotNull(tradeContentResult);
        Assert.Equal(count, tradeContentResult.Length);
    }*/

    [Fact(DisplayName = "Add several trade contents and get a list with trade's trade contents")]
    public async Task DeleteTradeItems_AddSeveralTradeContentsAndDeleteAllTradeContents_ReturnsTrue()
    {
        // Arrange

        int count = 5;
        string tradeId = Trade.GenerateId();

        for (int i = 0; i < count; i++)
        {
            string itemId = Item.GenerateId();
            int price = 5;
            int quantity = 2;

            var tradeContentMock = new TradeItem(tradeId, itemId, quantity, price);

            await _sut.AddEntityAsync(tradeContentMock);
        }

        await _contextWrapper.ProvideDatabaseContext().SaveChangesAsync();

        // Act

        var deleteTradeContentsResult = await _sut.DeleteTradeItemsAsync(tradeId);

        // Assert

        Assert.True(deleteTradeContentsResult);
    }
}
