using Domain.Aggregates.Trades;
using Domain.Entities.Trades;
using Domain.Repositories.Trades;
using Infrastructure.IntegrationTests.Common;
using Infrastructure.IntegrationTests.Utils;
using Infrastructure.Repositories.Trades;
using Infrastructure.Services.DatabaseContextWrapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.IntegrationTests.Database;

public class TradeTests : IClassFixture<DatabaseFixture>
{
    private readonly ITradeRepository _repository;
    private readonly IServiceProvider _serviceProvider;

    public TradeTests(DatabaseFixture fixture)
    {
        var dbContextWrapper = fixture.ServiceProvider.GetRequiredService<IDatabaseContextWrapper>();
        var sender = fixture.ServiceProvider.GetRequiredService<ISender>();
        _repository = new TradeRepository(dbContextWrapper, sender);
        _serviceProvider = fixture.ServiceProvider;
    }

    [Fact]
    public async Task CreateTrade_CreateNewTrade_ReturnsTrade()
    {
        // Arrange

        DateTime sentDate = DateTime.Now;
        var trade = new Trade(sentDate);

        (var senderUser, var receiverUser) = await TestingScenarios.CreateSenderReceiverUsersPair(_serviceProvider, 0);
        var item = await TestingScenarios.CreateItem(_serviceProvider, "Gold", string.Empty);
        var inventoryItem = await TestingScenarios.AddItemToUser(_serviceProvider, item, senderUser, 5);
        var tradeContent = new TradeItem(trade.TradeId, inventoryItem.ItemId, inventoryItem.Quantity, 10);

        trade.SetSender(senderUser.Id);
        trade.SetReceiver(receiverUser.Id);
        trade.AddTradeContent(tradeContent);

        // Act

        var tradeCreated = await _repository.AddEntityAsync(trade);
        var tradeEntity = await _repository.GetTradeEntityAsync(trade.TradeId);

        // Assert

        Assert.True(tradeCreated);
        Assert.NotNull(tradeEntity);
        Assert.Equal(trade.SentDate, tradeEntity.SentDate);
        Assert.Equal(trade.ResponseDate, tradeEntity.ResponseDate);
        Assert.Equal(trade.Response, tradeEntity.Response);
        Assert.Equal(trade.TradeId, tradeEntity.TradeId);
        Assert.Equal(trade.TradeContents.Count, tradeEntity.TradeContents.Count);
        Assert.All(trade.TradeContents, tc => tradeEntity.TradeContents.Contains(tc));
    }

    [Fact]
    public async Task AcceptTrade_CreateTradeThenAccept_AcceptsTrade()
    {
        // Arrange

        DateTime sentDate = DateTime.Now;
        var trade = new Trade(sentDate);

        (var senderUser, var receiverUser) = await TestingScenarios.CreateSenderReceiverUsersPair(_serviceProvider, 1);
        var item = await TestingScenarios.CreateItem(_serviceProvider, "Silver", string.Empty);
        var inventoryItem = await TestingScenarios.AddItemToUser(_serviceProvider, item, senderUser, 5);
        var tradeContent = new TradeItem(trade.TradeId, inventoryItem.ItemId, inventoryItem.Quantity, 10);

        trade.SetSender(senderUser.Id);
        trade.SetReceiver(receiverUser.Id);
        trade.AddTradeContent(tradeContent);

        // Act

        var tradeCreated = await _repository.AddEntityAsync(trade);
        trade.SetResponse(true);
        var tradeCanceled = await _repository.UpdateEntityAsync(trade);
        var retrievedTrade = await _repository.GetTradeEntityAsync(trade.TradeId);

        // Assert

        Assert.True(tradeCreated);
        Assert.True(tradeCanceled);
        Assert.NotNull(retrievedTrade);
        Assert.Equal(trade.TradeId, retrievedTrade.TradeId);
        Assert.Equal(trade.Response, retrievedTrade.Response);
        Assert.Equal(trade.ResponseDate, retrievedTrade.ResponseDate);
    }

    [Fact]
    public async Task RejectTrade_CreateTradeThenReject_RejectsTrade()
    {
        // Arrange

        DateTime sentDate = DateTime.Now;
        var trade = new Trade(sentDate);

        (var senderUser, var receiverUser) = await TestingScenarios.CreateSenderReceiverUsersPair(_serviceProvider, 2);
        var item = await TestingScenarios.CreateItem(_serviceProvider, "Copper", string.Empty);
        var inventoryItem = await TestingScenarios.AddItemToUser(_serviceProvider, item, senderUser, 5);
        var tradeContent = new TradeItem(trade.TradeId, inventoryItem.ItemId, inventoryItem.Quantity, 10);

        trade.SetSender(senderUser.Id);
        trade.SetReceiver(receiverUser.Id);
        trade.AddTradeContent(tradeContent);

        // Act

        var tradeCreated = await _repository.AddEntityAsync(trade);
        trade.SetResponse(false);
        var tradeCanceled = await _repository.UpdateEntityAsync(trade);
        var retrievedTrade = await _repository.GetTradeEntityAsync(trade.TradeId);

        // Assert

        Assert.True(tradeCreated);
        Assert.True(tradeCanceled);
        Assert.NotNull(retrievedTrade);
        Assert.Equal(trade.TradeId, retrievedTrade.TradeId);
        Assert.Equal(trade.Response, retrievedTrade.Response);
        Assert.Equal(trade.ResponseDate, retrievedTrade.ResponseDate);
    }

    [Fact]
    public async Task AddSentAndReceivedTradeEntities_CreateTradeThenAddSentAndReceivedTradeEntities_ExecutesSuccessfully()
    {
        // Arrange

        DateTime sentDate = DateTime.Now;
        var trade = new Trade(sentDate);

        (var senderUser, var receiverUser) = await TestingScenarios.CreateSenderReceiverUsersPair(_serviceProvider, 3);
        var item = await TestingScenarios.CreateItem(_serviceProvider, "Bronze", string.Empty);
        var inventoryItem = await TestingScenarios.AddItemToUser(_serviceProvider, item, senderUser, 5);
        var tradeContent = new TradeItem(trade.TradeId, inventoryItem.ItemId, inventoryItem.Quantity, 10);

        trade.SetSender(senderUser.Id);
        trade.SetReceiver(receiverUser.Id);
        trade.AddTradeContent(tradeContent);

        // Act

        var tradeCreated = await _repository.AddEntityAsync(trade);
        await _repository.AddSentAndReceivedTradeEntitiesAsync(trade.TradeId, senderUser.Id, receiverUser.Id);
        var sentTrade = await _repository.GetSentTradeEntityAsync(trade.TradeId);
        var receivedTrade = await _repository.GetReceivedTradeEntityAsync(trade.TradeId);

        // Assert

        Assert.True(tradeCreated);
        Assert.NotNull(sentTrade);
        Assert.NotNull(receivedTrade);
    }
}
