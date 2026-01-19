using Application.Behaviors.Identity.GetUsername;
using Application.Behaviors.Inventories.AddItem;
using Application.Behaviors.Inventories.DropItem;
using Application.Behaviors.Inventories.HasItem;
using Application.Behaviors.Item.GetItemName;
using Application.Behaviors.Trade.CancelTrade;
using Application.Behaviors.Trade.CreateTrade;
using Application.Behaviors.Trade.GetTrade;
using Application.Behaviors.Trade.ListTrades;
using Application.Behaviors.Trade.RespondTrade;
using Application.Behaviors.TradeItem.GetTradeItems;
using Application.Behaviors.TradeItem.RemoveTradeItems;
using Application.Behaviors.TradeItemHistory.AddTradeItems;
using Application.Behaviors.TradeItemHistory.GetTradeItems;
using Application.Behaviors.Wallet.GetCash;
using Application.Extensions;
using Application.Models.TradeItems;
using Application.Models.Trades;
using Application.Repositories;
using Application.Results.Inventories;
using Application.Results.TradeItemsHistory;
using Application.Results.Trades;
using Application.Services.Trade;
using Application.Services.UnitOfWork;
using Domain.Aggregates.Trades;
using Domain.Entities.Trades;
using MapsterMapper;
using MediatR;

namespace Application_UnitTests.Trades;

public class TradeServiceTests
{
    private readonly ITradeService _sut; // service under test
    private readonly IMapper _mapper;
    private readonly string senderUserId = Guid.NewGuid().ToString();
    private readonly string receiverUserId = Guid.NewGuid().ToString();
    private readonly string defaultUserName = "default_username";
    private readonly Dictionary<string, List<TradeItem>> currentTradeItems = new();
    private readonly List<Trade> collection;
    private readonly List<CachedTrade> cachedTrades;

    public TradeServiceTests()
    {
        collection = new List<Trade>();
        cachedTrades = new List<CachedTrade>();
        var tradeRepositoryMock = TestingUtils.CreateRepositoryMock<Trade, ICachedTradeRepository>(collection);
        _mapper = TestingUtils.GetMapper();
        var senderMock = new Mock<ISender>();
        var publisherMock = new Mock<IPublisher>();
        var unitOfWorkMock = new Mock<IUnitOfWorkService>();

        unitOfWorkMock.Setup(x => x.ExplicitTransaction(It.IsAny<Func<Task<bool>>>()))
            .Returns(async (Func<Task<bool>> func) =>
            {
                return await func();
            });

        unitOfWorkMock.Setup(x => x.ExplicitTransaction(It.IsAny<Func<TaskCompletionSource<TradeOfferResult?>, Task<bool>>>()))
            .Returns(async (Func<TaskCompletionSource<TradeOfferResult?>, Task<bool>> func) =>
            {
                var taskCompletionSource = new TaskCompletionSource<TradeOfferResult?>();
                var task = taskCompletionSource.Task;

                var commitTransaction = await func(taskCompletionSource);

                if (!task.IsCompleted && !task.IsCanceled)
                    taskCompletionSource.SetResult(null);

                return await task;
            });

        #region MediatorMocks

        tradeRepositoryMock.Setup(repo => repo.GetCachedTradeAsync(It.IsAny<string>()))
            .ReturnsAsync((string tradeId) =>
            {
                return GetCachedTrade(tradeId) ?? default;
            });

        tradeRepositoryMock.Setup(repo => repo.AddEntityAsync(It.IsAny<Trade>()))
            .Callback((Trade trade) =>
            {
                collection.Add(trade);

                cachedTrades.Add(new CachedTrade(
                    trade.TradeId,
                    string.Empty,
                    string.Empty,
                    trade.SentDate,
                    trade.Response,
                    trade.ResponseDate,
                    new TradeItemDTO[0])
                );
            });

        tradeRepositoryMock.Setup(repo => repo.AddSentAndReceivedTradeEntitiesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback((string tradeId, string senderUserId, string receiverUserId) =>
            {
                var cachedTrade = GetCachedTrade(tradeId);

                if (cachedTrade == null) return;

                cachedTrade.SenderUserId = senderUserId;
                cachedTrade.ReceiverUserId = receiverUserId;
            });

        /*tradeRepositoryMock.Setup(repo => repo.GetTradeEntityAsync(It.IsAny<string>()))
            .ReturnsAsync((string tradeId) =>
            {
                return collection.FirstOrDefault(x => x.TradeId == tradeId);
            });

        tradeRepositoryMock.Setup(repo => repo.GetSentTradeEntityAsync(It.IsAny<string>()))
            .ReturnsAsync((string tradeId) =>
            {
                var cachedTrade = GetCachedTrade(tradeId);

                if (cachedTrade == null) return null;

                return new SentTrade(tradeId, cachedTrade.SenderUserId);
            });

        tradeRepositoryMock.Setup(repo => repo.GetReceivedTradeEntityAsync(It.IsAny<string>()))
            .ReturnsAsync((string tradeId) =>
            {
                var cachedTrade = GetCachedTrade(tradeId);

                if (cachedTrade == null) return null;

                return new ReceivedTrade(tradeId, cachedTrade.ReceiverUserId);
            });*/

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
                return GetItemName(request.ItemId);
            });
        senderMock.Setup(x => x.Send(It.IsAny<GetUsernameQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetUsernameQuery request, CancellationToken ct) =>
            {
                return defaultUserName;
            });
        senderMock.Setup(x => x.Send(It.IsAny<GetUserCashQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetUserCashQuery request, CancellationToken ct) =>
            {
                return 500;
            });
        /*senderMock.Setup(x => x.Send(It.IsAny<AddTradeItemCommand>(), It.IsAny<CancellationToken>()))
            .Callback((AddTradeItemCommand request, CancellationToken ct) =>
            {
                var tradeContent = _mapper.AdaptToType<AddTradeItemCommand, TradeItem>(request);
                
            });*/
        senderMock.Setup(x => x.Send(It.IsAny<GetTradeItemsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetTradeItemsQuery request, CancellationToken ct) =>
            {
                return currentTradeItems[request.TradeId].Select(x => new TradeItem(x.TradeId, x.ItemId, x.Quantity, 0)).ToArray();
            });
        senderMock.Setup(x => x.Send(It.IsAny<GetTradeItemsHistoryQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetTradeItemsHistoryQuery request, CancellationToken ct) =>
            {
                return currentTradeItems[request.TradeId].Select(x => new TradeItem(x.TradeId, x.ItemId, x.Quantity, x.Price)).ToArray();
            });
        senderMock.Setup(x => x.Send(It.IsAny<AddTradeItemsHistoryCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AddTradeItemsHistoryCommand request, CancellationToken ct) =>
            {
                return new TradeItemHistoryResult { Success = true };
            });
        senderMock.Setup(x => x.Send(It.IsAny<RemoveTradeItemsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RemoveTradeItemsCommand request, CancellationToken ct) =>
            {
                return true;
            });
        senderMock.Setup(x => x.Send(It.IsAny<HasItemQuantityQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((HasItemQuantityQuery request, CancellationToken ct) =>
            {
                return true;
            });
        senderMock.Setup(x => x.Send(It.IsAny<AddInventoryItemCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AddInventoryItemCommand request, CancellationToken ct) =>
             {
                 return new QuantifiedItemResult
                 {
                     Success = true
                 };
             });
        senderMock.Setup(x => x.Send(It.IsAny<DropInventoryItemCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DropInventoryItemCommand request, CancellationToken ct) =>
            {
                return new QuantifiedItemResult
                {
                    Success = true
                };
            });
        
        /*cacheServiceMock.Setup(x => x.ListWithPrefix<TradeItem>(It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync((string prefix, bool removePrefix) =>
            {
                return new Dictionary<string, TradeItem>();
            });
        cacheServiceMock.Setup(x => x.ListWithPrefix<string>(It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync((string prefix, bool removePrefix) =>
            {
                return new Dictionary<string, string>();
            });*/

        #endregion MediatorMocks

        _sut = new TradeService(tradeRepositoryMock.Object, senderMock.Object, publisherMock.Object, _mapper, unitOfWorkMock.Object);
    }

    [Theory(DisplayName = "Create trade offer")]
    [InlineData("1")]
    [InlineData("1", "2", "3")]
    [InlineData("1", "2", "3", "4", "5")]
    public async Task CreateTradeOffer_CreateTradeOfferWithTradeItems_ReturnsCreatedTradeOffer(params string[] tradeItemIds)
    {
        // Arrange

        var tradeItems = TestingData.GetTradeItems(tradeItemIds);

        var commandStub = new CreateTradeOfferCommand
        {
            SenderUserId = senderUserId,
            TargetUserId = receiverUserId,
            Items = tradeItems.Select(x => _mapper.AdaptToType<TradeItem, TradeItemDTO>(x, (nameof(TradeItemDTO.ItemName), string.Empty)))
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
            Items = new List<TradeItemDTO>()
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
            currentTradeItems.Add(trade.TradeId, model.Items.Select(x => new TradeItem(trade.TradeId, x.ItemId, x.Quantity, x.Price)).ToList());

            var cachedTrade = cachedTrades.FirstOrDefault(x => x.TradeId == trade.TradeId);

            cachedTrade!.TradeItems = currentTradeItems[trade.TradeId].Select(x => _mapper.AdaptToType<TradeItem, TradeItemDTO>(x, (nameof(TradeItemDTO.ItemName), string.Empty))).ToArray();
        }

        return trade;
    }

    private async Task<List<string>> InitTradeOffersAndReturnIds(int numberOfTradeOffers, string[] tradeItemIds, bool responded = false)
    {
        List<string> tradeOfferIds = new();

        for (int i = 0; i < numberOfTradeOffers; i++)
        {
            var tradeItems = TestingData.GetTradeItems(tradeItemIds);

            var tradeOfferResult = await InitTrade(new CreateTradeOfferCommand
            {
                SenderUserId = senderUserId,
                TargetUserId = receiverUserId,
                Items = tradeItems.Select(x => _mapper.AdaptToType<TradeItem, TradeItemDTO>(x, (nameof(TradeItemDTO.ItemName), string.Empty)))
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
        var tradeItems = TestingData.GetTradeItems(tradeItemIds);

        var createTradeStub = new CreateTradeOfferCommand
        {
            SenderUserId = senderUserId,
            TargetUserId = receiverUserId,
            Items = tradeItems.Select(x => _mapper.AdaptToType<TradeItem, TradeItemDTO>(x, (nameof(TradeItemDTO.ItemName), string.Empty)))
        };

        return await InitTrade(createTradeStub);
    }

    private CachedTrade? GetCachedTrade(string tradeId) => cachedTrades.FirstOrDefault(x => x.TradeId == tradeId);

    private static string GetItemName(string itemId) => $"item_name_{itemId}";

    #endregion Utils
}
