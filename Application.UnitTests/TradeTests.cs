using Application.Behaviors.Identity.GetUsername;
using Application.Behaviors.Inventory.AddItem;
using Application.Behaviors.Inventory.DropItem;
using Application.Behaviors.Inventory.HasItem;
using Application.Behaviors.Item.GetItemName;
using Application.Behaviors.Trade.CancelTrade;
using Application.Behaviors.Trade.CreateTrade;
using Application.Behaviors.Trade.GetTrade;
using Application.Behaviors.Trade.ListTrades;
using Application.Behaviors.Trade.RespondTrade;
using Application.Behaviors.TradeItem.AddTradeItem;
using Application.Behaviors.TradeItem.GetTradeItems;
using Application.Behaviors.TradeItem.RemoveTradeItems;
using Application.Behaviors.TradeItemHistory.AddTradeItems;
using Application.Behaviors.TradeItemHistory.GetTradeItems;
using Application.Models.Inventory;
using Application.Models.Trade;
using Application.Models.TradeItemHistory;
using Application.Services.Trade;
using Application.Services.UnitOfWork;
using Domain.Repositories;
using Domain.Trade;
using Domain.TradeItems;
using Domain.Trades;
using MediatR;

namespace Application_UnitTests;

public class TradeTests
{
    private readonly ITradeService _sut; // service under test
    private readonly string senderUserId = Guid.NewGuid().ToString();
    private readonly string receiverUserId = Guid.NewGuid().ToString();
    private readonly string defaultUserName = "default_username";
    private readonly Dictionary<string, List<TradeItem>> currentTradeItems = new();
    private readonly List<Trade> collection;
    private readonly List<CachedTrade> cachedTrades;

    public TradeTests()
    {
        collection = new List<Trade>();
        cachedTrades = new List<CachedTrade>();
        var tradeRepositoryMock = TestingUtils.CreateRepositoryMock<Trade, ITradeRepository>(collection);
        var _mapper = TestingUtils.GetMapper();
        var senderMock = new Mock<ISender>();
        var publisherMock = new Mock<IPublisher>();
        var cacheServiceMock = TestingUtils.GetCacheServiceMock();
        var unitOfWorkMock = new Mock<IUnitOfWorkService>();

        #region MediatorMocks

        tradeRepositoryMock.Setup(repo => repo.GetCachedTradeAsync(It.IsAny<string>()))
            .ReturnsAsync((string tradeId) =>
            {
                return GetCachedTrade(tradeId) ?? new CachedTrade();
            });

        tradeRepositoryMock.Setup(repo => repo.AddEntityAsync(It.IsAny<Trade>()))
            .Callback((Trade trade) =>
            {
                collection.Add(trade);

                cachedTrades.Add(new CachedTrade
                {
                    TradeId = trade.TradeId,
                    SentDate = trade.SentDate,
                    ResponseDate = trade.ResponseDate,
                    Response = trade.Response
                });
            });

        tradeRepositoryMock.Setup(repo => repo.AddSentAndReceivedTradeEntitiesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback((string tradeId, string senderUserId, string receiverUserId) =>
            {
                var cachedTrade = GetCachedTrade(tradeId);

                if (cachedTrade == null) return;

                cachedTrade.SenderUserId = senderUserId;
                cachedTrade.ReceiverUserId = receiverUserId;
            });

        tradeRepositoryMock.Setup(repo => repo.GetTradeEntityAsync(It.IsAny<string>()))
            .ReturnsAsync((string tradeId) =>
            {
                return collection.FirstOrDefault(x => x.TradeId == tradeId);
            });

        tradeRepositoryMock.Setup(repo => repo.GetSentTradeEntityAsync(It.IsAny<string>()))
            .ReturnsAsync((string tradeId) =>
            {
                var cachedTrade = GetCachedTrade(tradeId);

                if (cachedTrade == null) return null;

                return new SentTrade
                {
                    TradeId = tradeId,
                    SenderId = cachedTrade.SenderUserId
                };
            });

        tradeRepositoryMock.Setup(repo => repo.GetReceivedTradeEntityAsync(It.IsAny<string>()))
            .ReturnsAsync((string tradeId) =>
            {
                var cachedTrade = GetCachedTrade(tradeId);

                if (cachedTrade == null) return null;

                return new ReceivedTrade
                {
                    TradeId = tradeId,
                    ReceiverId = cachedTrade.ReceiverUserId
                };
            });

        tradeRepositoryMock.Setup(repo => repo.GetTradeItemsAsync(It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync((string tradeId, bool responded) =>
            {
                if (!currentTradeItems.ContainsKey(tradeId)) return Array.Empty<TradeItem>();

                return currentTradeItems[tradeId].ToArray();
            });

        tradeRepositoryMock.Setup(repo => repo.ListReceivedTradeIdsCachedAsync(It.IsAny<string>()))
            .ReturnsAsync((string userId) =>
            {
                return cachedTrades.Where(x => x.ReceiverUserId == userId).Select(x => x.TradeId).ToArray();
            });

        tradeRepositoryMock.Setup(repo => repo.ListSentTradeIdsCachedAsync(It.IsAny<string>()))
            .ReturnsAsync((string userId) =>
            {
                return cachedTrades.Where(x => x.SenderUserId == userId).Select(x => x.TradeId).ToArray();
            });

        senderMock.Setup(x => x.Send(It.IsAny<IRequest<bool>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IRequest<bool> request, CancellationToken ct) =>
            {
                return true;
            });
        senderMock.Setup(x => x.Send(It.IsAny<IRequest<LockItemResult>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IRequest<LockItemResult> request, CancellationToken ct) =>
            {
                return new LockItemResult
                {
                    Success = true
                };
            });
        senderMock.Setup(x => x.Send(It.IsAny<GetItemNameQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetItemNameQuery request, CancellationToken ct) =>
            {
                return TestingData.GetTradeItemName(request.ItemId);
            });
        senderMock.Setup(x => x.Send(It.IsAny<GetUsernameQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetUsernameQuery request, CancellationToken ct) =>
            {
                return defaultUserName;
            });
        senderMock.Setup(x => x.Send(It.IsAny<IRequest<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IRequest<int> request, CancellationToken ct) =>
            {
                return 500;
            });
        senderMock.Setup(x => x.Send(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
            .Callback((IRequest request, CancellationToken ct) =>
            {
                var tradeContent = _mapper.From((AddTradeItemCommand)request).AdaptToType<TradeContent>();
                
            });
        senderMock.Setup(x => x.Send(It.IsAny<GetTradeItemsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IRequest<TradeItem[]> request, CancellationToken ct) =>
            {
                return currentTradeItems[((GetTradeItemsQuery)request).TradeId].Select(x => new TradeItem { ItemId = x.ItemId, Name = string.Empty, Price = 0, Quantity = x.Quantity}).ToArray();
            });
        senderMock.Setup(x => x.Send(It.IsAny<GetTradeItemsHistoryQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IRequest<TradeItem[]> request, CancellationToken ct) =>
            {
                return currentTradeItems[((GetTradeItemsHistoryQuery)request).TradeId].Select(x => new TradeItem { ItemId = x.ItemId, Name = string.Empty, Price = 0, Quantity = x.Quantity }).ToArray();
            });
        senderMock.Setup(x => x.Send(It.IsAny<AddTradeItemsHistoryCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IRequest<TradeItemHistoryBaseResult> request, CancellationToken ct) =>
            {
                return new TradeItemHistoryBaseResult { Success = true };
            });
        senderMock.Setup(x => x.Send(It.IsAny<RemoveTradeItemsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IRequest<bool> request, CancellationToken ct) =>
            {
                return true;
            });
        senderMock.Setup(x => x.Send(It.IsAny<HasItemQuantityQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IRequest<bool> request, CancellationToken ct) =>
            {
                return true;
            });
        senderMock.Setup(x => x.Send(It.IsAny<AddInventoryItemCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IRequest<QuantifiedItemResult> request, CancellationToken ct) =>
             {
                 return new QuantifiedItemResult
                 {
                     Success = true
                 };
             });
        senderMock.Setup(x => x.Send(It.IsAny<DropInventoryItemCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IRequest<QuantifiedItemResult> request, CancellationToken ct) =>
            {
                return new QuantifiedItemResult
                {
                    Success = true
                };
            });
        
        cacheServiceMock.Setup(x => x.ListWithPrefix<TradeItem>(It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync((string prefix, bool removePrefix) =>
            {
                return new Dictionary<string, TradeItem>();
            });
        cacheServiceMock.Setup(x => x.ListWithPrefix<string>(It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync((string prefix, bool removePrefix) =>
            {
                return new Dictionary<string, string>();
            });

        #endregion MediatorMocks

        _sut = new TradeService(tradeRepositoryMock.Object, cacheServiceMock.Object, senderMock.Object, publisherMock.Object, _mapper, unitOfWorkMock.Object);
    }

    [Theory(DisplayName = "Create trade offer")]
    [InlineData("1")]
    [InlineData("1", "2", "3")]
    [InlineData("1", "2", "3", "4", "5")]
    public async Task CreateTradeOffer_CreateTradeOfferWithTradeItems_ReturnsCreatedTradeOffer(params string[] tradeItemIds)
    {
        // Arrange

        var tradeItems = InitTradeItems(tradeItemIds);

        var commandStub = new CreateTradeOfferCommand
        {
            SenderUserId = senderUserId,
            TargetUserId = receiverUserId,
            Items = tradeItems
        };

        // Act

        var result = await InitTrade(commandStub);

        // Assert

        Assert.True(result.Success, "The result should be successful");
        Assert.False(string.IsNullOrEmpty(result.TradeId), "The trade offer id must not be empty or null");
        Assert.True(result.Items.All(x => tradeItemIds.Contains(x.ItemId)), "The trade offer's items should contain all of the inserted items");
        Assert.Equal(defaultUserName, result.ReceiverName);
    }

    [Fact(DisplayName = "Create trade offer without trade items")]
    public async Task CreateTradeOffer_CreateTradeOfferWithoutTradeItems_ShouldFail()
    {
        // Arrange

        var commandStub = new CreateTradeOfferCommand
        {
            SenderUserId = senderUserId,
            TargetUserId = receiverUserId,
            Items = new List<TradeItem>()
        };

        // Act

        var result = await InitTrade(commandStub);

        // Assert

        Assert.False(result.Success, "The result should be unsuccessful because no trade items were given");
    }

    [Theory(DisplayName = "Get sent trade")]
    [InlineData("1")]
    [InlineData("1", "2", "3")]
    public async Task GetTradeOffer_CreateTradeOfferThenGetSentTrade_ReturnsCreatedTradeOffer(params string[] tradeItemIds)
    {
        // Arrange

        var tradeOfferResult = await InitTradeWithTradeItems(tradeItemIds);

        var queryStub = new RequestTradeOfferQuery
        {
            TradeId = tradeOfferResult.TradeId
        };

        // Act

        var result = await _sut.GetTradeOfferAsync(queryStub);

        // Assert

        Assert.True(result.Success, "The result should be successful");
        Assert.Equal(tradeItemIds.Length, result.Items.Count());
        Assert.True(result.Items.All(x => tradeItemIds.Contains(x.ItemId)), "The result should contain all items that was inserted in the created trade");
        Assert.Equal(receiverUserId, result.ReceiverId);
    }

    [Theory(DisplayName = "Get sent trades")]
    [InlineData(1, "1")]
    [InlineData(5, "1", "2", "3")]
    public async Task GetTradeOffers_CreateSeveralTradeOffersThenGetSentTrades_ReturnsCreatedTradeOfferIds(int numberOfTradeOffers, params string[] tradeItemIds)
    {
        // Arrange

        List<string> tradeOfferIds = await InitTradeOffersAndReturnIds(numberOfTradeOffers, tradeItemIds);

        var queryStub = new ListTradesQuery
        {
            UserId = senderUserId,
            TradeDirection = TradeDirection.Sent
        };

        // Act

        var result = await _sut.GetTradeOffersAsync(queryStub);

        // Assert

        Assert.True(result.Success, "The result should be successful");
        Assert.True(tradeOfferIds.All(x => result.SentTradeOfferIds.Contains(x)), "The result should contain all the trade ids of the trades that were created");
    }

    [Theory(DisplayName = "Get responded sent trades")]
    [InlineData(1, "1")]
    [InlineData(5, "1", "2", "3")]
    public async Task GetTradeOffers_CreateSeveralTradeOffersThenRespondToThemAndThenGetRespondedSentTrades_ReturnsCreatedAndRespondedTradeOfferIds(int numberOfTradeOffers, params string[] tradeItemIds)
    {
        // Arrange

        List<string> tradeOfferIds = await InitTradeOffersAndReturnIds(numberOfTradeOffers, tradeItemIds, true);

        var stub = new ListTradesQuery
        {
            UserId = senderUserId,
            TradeDirection = TradeDirection.Sent,
            Responded = true
        };

        // Act

        var result = await _sut.GetTradeOffersAsync(stub);

        // Assert

        Assert.True(result.Success, "The result should be successful");
        Assert.True(result.SentTradeOfferIds.Count() == tradeOfferIds.Count, "The result's ids count should be equal to the count of trades that were created");
        Assert.True(tradeOfferIds.All(x => result.SentTradeOfferIds.Contains(x)), "The result should contain all the trade ids of the trades that were created");
    }

    [Theory(DisplayName = "Get received trade")]
    [InlineData("1")]
    [InlineData("1", "2", "3")]
    public async Task GetTradeOffer_CreateTradeOfferThenGetReceivedTrade_ReturnsCreatedTradeOffer(params string[] tradeItemIds)
    {
        // Arrange

        var tradeOfferResult = await InitTradeWithTradeItems(tradeItemIds);

        var queryStub = new RequestTradeOfferQuery
        {
            TradeId = tradeOfferResult.TradeId
        };

        // Act

        var result = await _sut.GetTradeOfferAsync(queryStub);

        // Assert

        Assert.True(result.Success, "The result should be successful");
        Assert.Equal(tradeItemIds.Length, result.Items.Count());
        Assert.True(result.Items.All(x => tradeItemIds.Contains(x.ItemId)), "The trade offer's items should contain all of the inserted items");
        Assert.Equal(senderUserId, result.SenderId);
    }

    [Theory(DisplayName = "Get received trades")]
    [InlineData(1, "1")]
    [InlineData(5, "1", "2", "3")]
    public async Task GetTradeOffers_CreateSeveralTradeOffersThenGetReceivedTrades_ReturnsCreatedTradeOfferIds(int numberOfTradeOffers, params string[] tradeItemIds)
    {
        // Arrange

        List<string> tradeOfferIds = await InitTradeOffersAndReturnIds(numberOfTradeOffers, tradeItemIds);

        var queryStub = new ListTradesQuery
        {
            UserId = receiverUserId,
            TradeDirection = TradeDirection.Received
        };

        // Act

        var result = await _sut.GetTradeOffersAsync(queryStub);

        // Assert

        Assert.True(result.Success, "The result should be successful");
        Assert.True(tradeOfferIds.All(x => result.ReceivedTradeOfferIds.Contains(x)), "The result should contain all the trade ids of the trades that were created");
    }

    [Theory(DisplayName = "Get responded received trades")]
    [InlineData(1, "1")]
    [InlineData(5, "1", "2", "3")]
    public async Task GetTradeOffers_CreateSeveralTradeOffersThenRespondToThemAndThenGetRespondedReceivedTrades_ReturnsCreatedTradeOfferIds(int numberOfTradeOffers, params string[] tradeItemIds)
    {
        // Arrange

        List<string> tradeOfferIds = await InitTradeOffersAndReturnIds(numberOfTradeOffers, tradeItemIds, true);

        var queryStub = new ListTradesQuery
        {
            UserId = receiverUserId,
            TradeDirection = TradeDirection.Received,
            Responded = true
        };

        // Act

        var result = await _sut.GetTradeOffersAsync(queryStub);

        // Assert

        Assert.True(result.Success, "The result should be successful");
        Assert.True(result.ReceivedTradeOfferIds.Count() == tradeOfferIds.Count, "The result's ids count should be equal to the count of trades that were created");
        Assert.True(tradeOfferIds.All(x => result.ReceivedTradeOfferIds.Contains(x)), "The result should contain all the trade ids of the trades that were created");
    }

    [Theory(DisplayName = "Accept trade")]
    [InlineData("1")]
    [InlineData("1", "2", "3")]
    [InlineData("1", "2", "3", "4", "5")]
    public async Task AcceptTradeOffer_CreateTradeThenAcceptTradeOffer_ReturnsAcceptedTradeOffer(params string[] tradeItemIds)
    {
        // Arrange
        
        var tradeOfferResult = await InitTradeWithTradeItems(tradeItemIds);

        var commandStub = new RespondTradeCommand
        {
            TradeId = tradeOfferResult.TradeId,
            UserId = receiverUserId
        };

        // Act

        var result = await _sut.AcceptTradeOfferAsync(commandStub);

        // Assert

        Assert.True(result.Success, "Result should be successful");
        Assert.Equal(tradeOfferResult.TradeId, result.TradeId);
        Assert.Equal(senderUserId, result.SenderId);
    }

    [Theory(DisplayName = "Reject trade")]
    [InlineData("1")]
    [InlineData("1", "2", "3")]
    [InlineData("1", "2", "3", "4", "5")]
    public async Task RejectTradeOffer_CreateTradeOfferThenRejectTradeOffer_ReturnsRejectedTradeOffer(params string[] tradeItemIds)
    {
        // Arrange

        var tradeOfferResult = await InitTradeWithTradeItems(tradeItemIds);

        var commandStub = new RespondTradeCommand
        {
            TradeId = tradeOfferResult.TradeId,
            UserId = receiverUserId
        };

        // Act

        var result = await _sut.RejectTradeOfferAsync(commandStub);

        // Assert

        Assert.True(result.Success, "Result should be successful");
        Assert.Equal(tradeOfferResult.TradeId, result.TradeId);
        Assert.Equal(senderUserId, result.SenderId);
    }

    [Theory(DisplayName = "Cancel trade")]
    [InlineData("1")]
    [InlineData("1", "2", "3")]
    [InlineData("1", "2", "3", "4", "5")]
    public async Task CancelTradeOffer_CreateTradeOfferThenCancelTradeOffer_ReturnsCancelledTradeOffer(params string[] tradeItemIds)
    {
        // Arrange

        var tradeOfferResult = await InitTradeWithTradeItems(tradeItemIds);

        var commandStub = new CancelTradeCommand
        {
            TradeId = tradeOfferResult.TradeId,
            UserId = senderUserId
        };

        // Act

        var result = await _sut.CancelTradeOfferAsync(commandStub);

        // Assert

        Assert.True(result.Success, "Result should be successful");
        Assert.Equal(tradeOfferResult.TradeId, result.TradeId);
        Assert.Equal(receiverUserId, result.ReceiverId);
    }

    #region Utils

    private async Task<TradeOfferResult> InitTrade(CreateTradeOfferCommand model)
    {
        var trade = await _sut.CreateTradeOfferAsync(model);

        if (model.Items is not null && model.Items.Any())
        {
            currentTradeItems.Add(trade.TradeId, model.Items.Select(x => new TradeItem { ItemId = x.ItemId, Quantity = x.Quantity }).ToList());

            var cachedTrade = cachedTrades.FirstOrDefault(x => x.TradeId == trade.TradeId);

            cachedTrade!.TradeItemsId = currentTradeItems[trade.TradeId].Select(x => x.ItemId).ToArray();
        }

        return trade;
    }

    private static TradeItem[] InitTradeItems(string[] tradeItemIds)
    {
        var tradeItems = TestingData.GetTradeItems(tradeItemIds);

        for (int i = 0; i < tradeItemIds.Length; i++)
        {
            tradeItems[i].Price += i;
            tradeItems[i].Quantity += i * 2;
        }

        return tradeItems;
    }

    private async Task<List<string>> InitTradeOffersAndReturnIds(int numberOfTradeOffers, string[] tradeItemIds, bool responded = false)
    {
        List<string> tradeOfferIds = new();

        for (int i = 0; i < numberOfTradeOffers; i++)
        {
            var tradeItems = InitTradeItems(tradeItemIds);

            var tradeOfferResult = await InitTrade(new CreateTradeOfferCommand
            {
                SenderUserId = senderUserId,
                TargetUserId = receiverUserId,
                Items = tradeItems
            });

            if (responded)
                await _sut.AcceptTradeOfferAsync(new RespondTradeCommand
                {
                    TradeId = tradeOfferResult.TradeId,
                    UserId = receiverUserId
                });

            tradeOfferIds.Add(tradeOfferResult.TradeId);
        }

        return tradeOfferIds;
    }

    private async Task<TradeOfferResult> InitTradeWithTradeItems(string[] tradeItemIds)
    {
        var tradeItems = InitTradeItems(tradeItemIds);

        var createTradeStub = new CreateTradeOfferCommand
        {
            SenderUserId = senderUserId,
            TargetUserId = receiverUserId,
            Items = tradeItems
        };

        return await InitTrade(createTradeStub);
    }

    private CachedTrade? GetCachedTrade(string tradeId) => cachedTrades.FirstOrDefault(x => x.TradeId == tradeId);

    #endregion Utils
}
