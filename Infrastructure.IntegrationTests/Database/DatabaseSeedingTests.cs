using Domain.Aggregates.Trades;
using Infrastructure.Extensions;
using Infrastructure.IntegrationTests.Common.Fixtures;
using Infrastructure.Services.DatabaseContextWrapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.IntegrationTests.Database;

public class DatabaseSeedingTests : IClassFixture<DatabaseFixture>
{
    private readonly IDatabaseContextWrapper databaseContextWrapper;
    private readonly IServiceProvider serviceProvider;

    public DatabaseSeedingTests(DatabaseFixture fixture)
    {
        databaseContextWrapper = fixture.ServiceProvider.GetRequiredService<IDatabaseContextWrapper>();
        serviceProvider = fixture.ServiceProvider;
    }

    [Fact(DisplayName = "Seed database")]
    public async Task SeedDatabase()
    {
        // Arrange

        var expectedUsersCount = 2;
        var expectedItemsCount = 5;
        var expectedInventoriesCount = 2;


        var output = new StringWriter();
        var originalOutput = Console.Out;
        Console.SetOut(output);

        var databaseContext = await databaseContextWrapper.ProvideDatabaseContextAsync();

        // Act

        await databaseContext.SeedDatabase(serviceProvider);
        var consoleContent = output.ToString();

        // Assert

        Assert.DoesNotContain("Failed to seed the database", consoleContent);
        // There should be two users
        var users = databaseContext.Users.ToList();
        Assert.NotEmpty(users);
        Assert.Equal(expectedUsersCount, users.Count);
        // Two different users
        var userIds = users.Select(x => x.Id).Distinct().ToList();
        Assert.Equal(expectedUsersCount, userIds.Count);
        // Five items were created
        var items = databaseContext.Items.ToList();
        Assert.NotEmpty(items);
        Assert.Equal(expectedItemsCount, items.Count);
        // Five different items
        var itemIds = items.Select(x => x.ItemId).Distinct().ToList();
        Assert.Equal(expectedItemsCount, itemIds.Count);
        Assert.All(items, item =>
        {
            Assert.NotEmpty(item.Name);
            Assert.NotEmpty(item.Description);
        });
        // There must be two inventories
        var inventories = databaseContext.Inventories.ToList();
        Assert.NotEmpty(inventories);
        Assert.Equal(expectedInventoriesCount, inventories.Count);
        // Two different inventories
        var inventoryUserIds = inventories.Select(x => x.UserId).Distinct().ToList();
        Assert.Equal(expectedInventoriesCount, inventoryUserIds.Count);
        // Each user has an inventory
        Assert.All(inventoryUserIds, inventoryUserId => Assert.Contains(inventoryUserId, userIds));
        // There are a few trades
        var trades = databaseContext.Trades
            .Include(x => x.SentTrade)
            .Include(x => x.ReceivedTrade)
            .Include(x => x.TradeContents)
            .ToList();
        Assert.NotEmpty(trades);
        // Users sent trades to eachother
        var tradesGroupedBySender = trades.GroupBy(x => x.SenderId).ToDictionary(x => x.Key, x => x.ToList());
        // Each user has sent at least one trade
        Assert.All(userIds, userId => tradesGroupedBySender.ContainsKey(userId));
        var firstUserId = userIds[0];
        var firstUserTrades = tradesGroupedBySender[userIds[0]];
        Assert.NotNull(firstUserTrades);
        var secondUserId = userIds[1];
        var secondUserTrades = tradesGroupedBySender[userIds[1]];
        Assert.NotNull(secondUserTrades);
        // Assert first user's trades
        Assert.All(firstUserTrades, trade =>
        {
            AssertTrade(trade, firstUserId, secondUserId, itemIds);
        });
        // Assert second user's trades
        Assert.All(secondUserTrades, trade =>
        {
            AssertTrade(trade, secondUserId, firstUserId, itemIds);
        });
    }

    private void AssertTrade(Trade trade, string senderUserId, string receiverUserId, List<string> itemIds)
    {
        Assert.NotNull(trade);
        Assert.NotEmpty(trade.TradeId);
        Assert.Equal(senderUserId, trade.GetSenderId());
        Assert.Equal(receiverUserId, trade.GetReceiverId());
        Assert.True(trade.GetTotalPrice() > 0);
        Assert.Null(trade.Response);
        Assert.Null(trade.ResponseDate);
        Assert.NotNull(trade.TradeContents);
        AssertTradeContents(trade, itemIds);
    }

    private void AssertTradeContents(Trade trade, List<string> itemIds)
    {
        Assert.All(trade.TradeContents, tradeContent =>
        {
            // must be an existing item
            Assert.Contains(tradeContent.ItemId, itemIds);
            Assert.Equal(trade.TradeId, tradeContent.TradeId);
            Assert.True(tradeContent.Quantity > 0);
            Assert.True(tradeContent.Price > 0);
        });
    }
}
