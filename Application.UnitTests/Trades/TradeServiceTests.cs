using Application.Behaviors.Identity.GetUsername;
using Application.Behaviors.Inventories.AddItem;
using Application.Behaviors.Inventories.DropItem;
using Application.Behaviors.Inventories.HasItem;
using Application.Behaviors.Inventories.LockItems;
using Application.Behaviors.Inventories.UnlockItem;
using Application.Behaviors.Item.GetItemName;
using Application.Behaviors.Trade.CancelTrade;
using Application.Behaviors.Trade.CreateTrade;
using Application.Behaviors.Trade.GetTrade;
using Application.Behaviors.Trade.ListTrades;
using Application.Behaviors.Trade.RespondTrade;
using Application.Behaviors.Wallet.GetCash;
using Application.Models.TradeItems;
using Application.Models.Trades;
using Application.Repositories;
using Application.Results.Inventories;
using Application.Results.Trades;
using Application.Services.Trades;
using Application.Services.UnitOfWork;
using Domain.Aggregates.Inventories;
using Domain.Aggregates.Trades;
using Domain.Entities.Identity;
using Domain.Entities.Trades;
using MapsterMapper;
using MediatR;

namespace Application_UnitTests.Trades;

public class TradeServiceTests
{
    private readonly ITradeService _sut; // service under test
    private readonly IMapper _mapper;
    private readonly Mock<ISender> _sender;
    private readonly string senderUserId = Guid.NewGuid().ToString();
    private readonly string receiverUserId = Guid.NewGuid().ToString();
    private readonly string defaultUserName = "default_username";    
    private readonly Dictionary<string, Inventory> inventories = new(); // <userId, Inventory>
    private readonly Dictionary<string, List<TradeItem>> currentTradeItems = new();
    private readonly List<Trade> collection;
    private readonly List<CachedTrade> cachedTrades;

    public TradeServiceTests()
    {
        collection = new List<Trade>();
        cachedTrades = new List<CachedTrade>();
        var tradeRepositoryMock = TestingUtils.CreateRepositoryMock<Trade, ICachedTradeRepository>(collection);
        _mapper = TestingUtils.GetMapper();
        _sender = new Mock<ISender>();
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

        tradeRepositoryMock.Setup(repo => repo.GetTradeAsync(It.IsAny<string>()))
            .ReturnsAsync((string tradeId) =>
            {
                var cachedTrade = GetCachedTrade(tradeId);

                if (cachedTrade is null)
                 return null;

                var trade = new Trade(cachedTrade.TradeId, cachedTrade.SentDate, cachedTrade.ResponseDate, cachedTrade.Response, cachedTrade.SenderUserId, cachedTrade.ReceiverUserId);
                
                foreach(var tradeItem in cachedTrade.TradeItems)
                {
                    trade.AddTradeContent(
                        new TradeItem(
                            cachedTrade.TradeId,
                            tradeItem.ItemId,
                            tradeItem.Quantity,
                            tradeItem.Price
                        )
                    );
                }

                return trade;
            });

        tradeRepositoryMock.Setup(repo => repo.GetTradeResponseAsync(It.IsAny<string>()))
            .ReturnsAsync((string tradeId) =>
            {
                var trade = GetTrade(tradeId);

                if (trade is null)
                    return null;

                return trade.Response;
            });

        tradeRepositoryMock.Setup(repo => repo.AddEntityAsync(It.IsAny<Trade>()))
            .ReturnsAsync((Trade trade) =>
            {
                collection.Add(trade);

                cachedTrades.Add(new CachedTrade(
                    trade.TradeId,
                    trade.SentTrade.SenderId,
                    trade.ReceivedTrade.ReceiverId,
                    trade.SentDate,
                    trade.Response,
                    trade.ResponseDate,
                    new TradeItem[0])
                );

                return true;
            });

        tradeRepositoryMock.Setup(repo => repo.UpdateEntityAsync(It.IsAny<Trade>()))
            .ReturnsAsync((Trade trade) =>
            {
                var index = collection.FindIndex(x => x.TradeId == trade.TradeId);

                if (index == -1)
                    return false;

                collection[index] = trade;

                index = cachedTrades.FindIndex(x => x.TradeId == trade.TradeId);

                if (index == -1)
                    return false;

                cachedTrades[index] = new CachedTrade(
                    trade.TradeId,
                    trade.SentTrade.SenderId,
                    trade.ReceivedTrade.ReceiverId,
                    trade.SentDate,
                    trade.Response,
                    trade.ResponseDate,
                    new TradeItem[0]);

                return true;
            });

        tradeRepositoryMock.Setup(repo => repo.RemoveEntityAsync(It.IsAny<Trade>()))
            .ReturnsAsync((Trade trade) =>
            {
                var index = collection.FindIndex(x => x.TradeId == trade.TradeId);

                if (index == -1)
                    return false;

                collection.RemoveAt(index);

                index = cachedTrades.FindIndex(x => x.TradeId == trade.TradeId);

                if (index == -1)
                    return false;

                cachedTrades.RemoveAt(index);

                return true;
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

        tradeRepositoryMock.Setup(repo => repo.SaveChangesAsync())
            .ReturnsAsync(() => 1);

        tradeRepositoryMock.Setup(repo => repo.MoveTradeContentToHistory(It.IsAny<string>()))
            .ReturnsAsync(() => true);

        _sender.Setup(x => x.Send(It.IsAny<IRequest<bool>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IRequest<bool> request, CancellationToken ct) =>
            {
                return true;
            });
        _sender.Setup(x => x.Send(It.IsAny<IRequest<LockItemResult>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IRequest<LockItemResult> request, CancellationToken ct) =>
            {
                return new LockItemResult
                {
                    Success = true
                };
            });
        _sender.Setup(x => x.Send(It.IsAny<IRequest<LockItemsResult>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IRequest<LockItemsResult> request, CancellationToken ct) =>
            {
                var command = (request as LockItemsCommand)!;
                
                if (!HasInventory(command.UserId))
                {
                    return new LockItemsResult
                    {
                        Errors = new string[] { "User has no inventory" }
                    };
                }

                var inventory = GetInventory(command.UserId);
                
                return new LockItemsResult
                {
                    Success = true,
                    Items = command.Items
                        .Select(item =>
                        {
                            inventory.LockItem(item.itemId, item.quantity);

                            return new TradeItemDTO
                            {
                                ItemId = item.itemId,
                                Quantity = inventory.GetItemFreeAmount(item.itemId)
                            };
                        })
                        .ToArray()
                };
            });
        _sender.Setup(x => x.Send(It.IsAny<GetItemNameQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetItemNameQuery request, CancellationToken ct) =>
            {
                return GetItemName(request.ItemId);
            });
        _sender.Setup(x => x.Send(It.IsAny<GetUsernameQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetUsernameQuery request, CancellationToken ct) =>
            {
                return defaultUserName;
            });
        _sender.Setup(x => x.Send(It.IsAny<GetUserCashQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetUserCashQuery request, CancellationToken ct) =>
            {
                return 500;
            });
        _sender.Setup(x => x.Send(It.IsAny<HasItemQuantityQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((HasItemQuantityQuery request, CancellationToken ct) =>
            {
                return true;
            });
        _sender.Setup(x => x.Send(It.IsAny<AddInventoryItemCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AddInventoryItemCommand request, CancellationToken ct) =>
             {
                 return new QuantifiedItemResult
                 {
                     Success = true
                 };
             });
        _sender.Setup(x => x.Send(It.IsAny<DropInventoryItemCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DropInventoryItemCommand request, CancellationToken ct) =>
            {
                return new QuantifiedItemResult
                {
                    Success = true
                };
            });
        #endregion MediatorMocks

        _sut = new TradeService(tradeRepositoryMock.Object, _sender.Object, publisherMock.Object, _mapper, unitOfWorkMock.Object);
    }

    [Theory(DisplayName = "Create trade offer")]
    [InlineData("1")]
    [InlineData("1", "2", "3")]
    [InlineData("1", "2", "3", "4", "5")]
    public async Task CreateTradeOffer_CreateTradeOfferWithTradeItems_ReturnsCreatedTradeOffer(params string[] tradeItemIds)
    {
        // Arrange

        var userId = User.GenerateId();

        var inventory = GetInventory(userId);

        var expectedItemsCount = tradeItemIds.Length;
        var expectedQuantity = 10;
        var expectedPrice = 10;

        var tradeItemDTOs = new TradeItemDTO[expectedItemsCount];

        for (int i = 0; i < expectedItemsCount; i++)
        {
            var itemId = tradeItemIds[i];

            inventory.AddItem(itemId, expectedQuantity);

            tradeItemDTOs[i] = new TradeItemDTO
            {
                ItemId = itemId,
                ItemName = GetItemName(itemId),
                Quantity = expectedQuantity,
                Price = expectedPrice
            };
        }

        var commandStub = new CreateTradeOfferCommand
        {
            SenderUserId = userId,
            TargetUserId = receiverUserId,
            Items = tradeItemDTOs
        };

        // Act

        var result = await InitTrade(commandStub);

        // Assert

        Assert.True(result.Success, "The result should be successful");
        Assert.False(string.IsNullOrEmpty(result.TradeId), "The trade offer id must not be empty or null");
        Assert.True(result.Items.All(x => tradeItemIds.Contains(x.ItemId)), "The trade offer's items should contain all of the inserted items");
        Assert.Equal(defaultUserName, result.ReceiverName);
        Assert.Equal(defaultUserName, result.SenderName);
        Assert.Equal(userId, result.SenderId);
        Assert.Equal(receiverUserId, result.ReceiverId);
        Assert.Null(result.Response);
        Assert.Null(result.ResponseDate);
        Assert.All(result.Items, (item) =>
        {
            Assert.Contains(commandStub.Items, x =>  x.ItemId == item.ItemId &&
                // x.ItemName == item.ItemName && 
                x.Quantity == item.Quantity &&
                x.Price == item.Price
            );
        });
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
        Assert.Single(result.Errors);
        Assert.NotEmpty(result.Errors.FirstOrDefault()!);
    }

    [Theory(DisplayName = "Get sent trade")]
    [InlineData("1")]
    [InlineData("1", "2", "3")]
    public async Task GetTradeOffer_CreateTradeOfferThenGetSentTrade_ReturnsCreatedTradeOffer(params string[] tradeItemIds)
    {
        // Arrange

        var userId = User.GenerateId();

        var tradeOfferResult = await InitTradeWithTradeItems(userId, tradeItemIds, 10, 10);

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
        Assert.True(result.Items.All(x => tradeItemIds.Contains(x.ItemId)), "The trade offer's items should contain all of the inserted items");
        Assert.Equal(defaultUserName, result.ReceiverName);
        Assert.Equal(defaultUserName, result.SenderName);
        Assert.Equal(userId, result.SenderId);
        Assert.Equal(receiverUserId, result.ReceiverId);
        Assert.Null(result.Response);
        Assert.Null(result.ResponseDate);
        Assert.All(result.Items, (item) =>
        {
            Assert.Contains(tradeItemIds, x =>  x == item.ItemId);
        });
    }

    [Theory(DisplayName = "Get sent trades")]
    [InlineData(1, "1")]
    [InlineData(5, "1", "2", "3")]
    public async Task GetTradeOffers_CreateSeveralTradeOffersThenGetSentTrades_ReturnsCreatedTradeOfferIds(int numberOfTradeOffers, params string[] tradeItemIds)
    {
        // Arrange

        var senderUserId = User.GenerateId();

        List<string> tradeOfferIds = await InitTradeOffersAndReturnIds(senderUserId, numberOfTradeOffers, tradeItemIds);

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

        var senderUserId = User.GenerateId();

        List<string> tradeOfferIds = await InitTradeOffersAndReturnIds(senderUserId, numberOfTradeOffers, tradeItemIds, true);

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

        var userId = User.GenerateId();

        var tradeOfferResult = await InitTradeWithTradeItems(userId, tradeItemIds, 10, 10);

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
        Assert.Equal(defaultUserName, result.ReceiverName);
        Assert.Equal(defaultUserName, result.SenderName);
        Assert.Equal(userId, result.SenderId);
        Assert.Equal(receiverUserId, result.ReceiverId);
        Assert.Null(result.Response);
        Assert.Null(result.ResponseDate);
        Assert.All(result.Items, (item) =>
        {
            Assert.Contains(tradeOfferResult.Items, x =>  x.ItemId == item.ItemId &&
                // x.ItemName == item.ItemName && 
                x.Quantity == item.Quantity &&
                x.Price == item.Price
            );
        });
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

        var userId = User.GenerateId();

        List<string> tradeOfferIds = await InitTradeOffersAndReturnIds(userId, numberOfTradeOffers, tradeItemIds, true);

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

    [Theory(DisplayName = "Get all responded trades")]
    [InlineData(1, "1")]
    [InlineData(5, "1", "2", "3")]
    public async Task GetTradeOffers_CreateSeveralTradeOffersThenGetSentAndReceivedTrades_ReturnsCreatedTradeOfferIds(int numberOfTradeOffers, params string[] tradeItemIds)
    {
        // Arrange

        var userId = User.GenerateId();

        List<string> userCreatedTradeOfferIds = await InitTradeOffersAndReturnIds(userId, numberOfTradeOffers, tradeItemIds);
        List<string> receiverCreatedTradeOfferIds = await InitTradeOffersAndReturnIds(receiverUserId, userId, numberOfTradeOffers, tradeItemIds);

        var queryStub = new ListTradesQuery
        {
            UserId = receiverUserId,
            TradeDirection = TradeDirection.All
        };

        // Act

        var result = await _sut.GetTradeOffersAsync(queryStub);

        // Assert

        Assert.True(result.Success, "The result should be successful");
        Assert.True(result.ReceivedTradeOfferIds.Count() == userCreatedTradeOfferIds.Count, "The result's ids count should be equal to the count of trades that were created");
        Assert.True(userCreatedTradeOfferIds.All(x => result.ReceivedTradeOfferIds.Contains(x)), "The result should contain all the trade ids of the trades that were created");
        Assert.True(result.SentTradeOfferIds.Count() == receiverCreatedTradeOfferIds.Count, "The result's ids count should be equal to the count of trades that were created");
        Assert.True(receiverCreatedTradeOfferIds.All(x => result.SentTradeOfferIds.Contains(x)), "The result should contain all the trade ids of the trades that were created");
    }

    [Theory(DisplayName = "Get all responded trades")]
    [InlineData(1, "1")]
    [InlineData(5, "1", "2", "3")]
    public async Task GetTradeOffers_CreateSeveralTradeOffersThenRespondToThemAndThenGetRespondedSentAndReceivedTrades_ReturnsCreatedTradeOfferIds(int numberOfTradeOffers, params string[] tradeItemIds)
    {
        // Arrange

        var userId = User.GenerateId();

        List<string> userCreatedTradeOfferIds = await InitTradeOffersAndReturnIds(userId, numberOfTradeOffers, tradeItemIds, true);
        List<string> receiverCreatedTradeOfferIds = await InitTradeOffersAndReturnIds(receiverUserId, userId, numberOfTradeOffers, tradeItemIds, true);

        var queryStub = new ListTradesQuery
        {
            UserId = receiverUserId,
            TradeDirection = TradeDirection.All,
            Responded = true
        };

        // Act

        var result = await _sut.GetTradeOffersAsync(queryStub);

        // Assert

        Assert.True(result.Success, "The result should be successful");
        Assert.True(result.ReceivedTradeOfferIds.Count() == userCreatedTradeOfferIds.Count, "The result's ids count should be equal to the count of trades that were created");
        Assert.True(userCreatedTradeOfferIds.All(x => result.ReceivedTradeOfferIds.Contains(x)), "The result should contain all the trade ids of the trades that were created");
        Assert.True(result.SentTradeOfferIds.Count() == receiverCreatedTradeOfferIds.Count, "The result's ids count should be equal to the count of trades that were created");
        Assert.True(receiverCreatedTradeOfferIds.All(x => result.SentTradeOfferIds.Contains(x)), "The result should contain all the trade ids of the trades that were created");
    }

    [Theory(DisplayName = "Accept trade")]
    [InlineData("1")]
    [InlineData("1", "2", "3")]
    [InlineData("1", "2", "3", "4", "5")]
    public async Task AcceptTradeOffer_CreateTradeThenAcceptTradeOffer_ReturnsAcceptedTradeOffer(params string[] tradeItemIds)
    {
        // Arrange

        var userId = User.GenerateId();

        var tradeOfferResult = await InitTradeWithTradeItems(userId, tradeItemIds, 10, 10);

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
        Assert.Equal(defaultUserName, result.ReceiverName);
        Assert.Equal(defaultUserName, result.SenderName);
        Assert.Equal(userId, result.SenderId);
        Assert.Equal(receiverUserId, result.ReceiverId);
        Assert.True(result.Response);
        Assert.NotNull(result.ResponseDate);
        Assert.All(result.Items, (item) =>
        {
            Assert.Contains(tradeOfferResult.Items, x =>  x.ItemId == item.ItemId &&
                // x.ItemName == item.ItemName && 
                x.Quantity == item.Quantity &&
                x.Price == item.Price
            );
        });
    }

    [Fact]
    public async Task AcceptTradeOffer_CreateTradeAcceptTradeOfferThenTryToAcceptAgain_ShouldFail()
    {
        // Arrange

        var userId = User.GenerateId();

        var tradeItemIds = new string[] { "1", "2", "3" };

        var tradeOfferResult = await InitTradeWithTradeItems(userId, tradeItemIds, 10, 10);

        var commandStub = new RespondTradeCommand
        {
            TradeId = tradeOfferResult.TradeId,
            UserId = receiverUserId
        };

        // Act

        await _sut.AcceptTradeOfferAsync(commandStub);
        var result = await _sut.AcceptTradeOfferAsync(commandStub);

        // Assert

        Assert.False(result.Success, "Result should fail");
        Assert.Single(result.Errors);
        Assert.NotEmpty(result.Errors.FirstOrDefault()!);
    }

    [Fact]
    public async Task AcceptTradeOffer_CreateTradeThenTryToAcceptTradeOfferAsSender_ShouldFail()
    {
        // Arrange

        var userId = User.GenerateId();
        var tradeItemIds = new string[] { "1" };

        var tradeOfferResult = await InitTradeWithTradeItems(userId, tradeItemIds, 10, 10);

        var commandStub = new RespondTradeCommand
        {
            TradeId = tradeOfferResult.TradeId,
            UserId = userId
        };

        // Act

        var result = await _sut.AcceptTradeOfferAsync(commandStub);

        // Assert

        Assert.False(result.Success, "Result should fail");
        Assert.Single(result.Errors);
        Assert.NotEmpty(result.Errors.FirstOrDefault()!);
    }

    [Theory(DisplayName = "Reject trade")]
    [InlineData("1")]
    [InlineData("1", "2", "3")]
    [InlineData("1", "2", "3", "4", "5")]
    public async Task RejectTradeOffer_CreateTradeOfferThenRejectTradeOffer_ReturnsRejectedTradeOffer(params string[] tradeItemIds)
    {
        // Arrange

        var userId = User.GenerateId();

        var tradeOfferResult = await InitTradeWithTradeItems(userId, tradeItemIds, 10, 10);

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
        Assert.Equal(userId, result.SenderId);
        Assert.Equal(defaultUserName, result.ReceiverName);
        Assert.Equal(defaultUserName, result.SenderName);
        Assert.Equal(userId, result.SenderId);
        Assert.Equal(receiverUserId, result.ReceiverId);
        Assert.False(result.Response);
        Assert.NotNull(result.ResponseDate);
        Assert.All(result.Items, (item) =>
        {
            Assert.Contains(tradeOfferResult.Items, x =>  x.ItemId == item.ItemId &&
                // x.ItemName == item.ItemName && 
                x.Quantity == item.Quantity &&
                x.Price == item.Price
            );
        });
    }

    [Fact]
    public async Task RejectTradeOffer_CreateTradeOfferRejectTradeOfferThenRejectItAgain_ShouldFail()
    {
        // Arrange

        var userId = User.GenerateId();

        var tradeItemIds = new string[] { "1", "2", "3" };

        var tradeOfferResult = await InitTradeWithTradeItems(userId, tradeItemIds, 10, 10);

        var commandStub = new RespondTradeCommand
        {
            TradeId = tradeOfferResult.TradeId,
            UserId = receiverUserId
        };

        // Act

        await _sut.RejectTradeOfferAsync(commandStub);
        var result = await _sut.RejectTradeOfferAsync(commandStub);

        // Assert

        Assert.False(result.Success, "Result should fail");
        Assert.Single(result.Errors);
        Assert.NotEmpty(result.Errors.FirstOrDefault()!);
    }

    [Fact]
    public async Task RejectTradeOffer_CreateTradeThenTryToRejectTradeOfferAsSender_ShouldFail()
    {
        // Arrange

        var userId = User.GenerateId();
        var tradeItemIds = new string[] { "1" };

        var tradeOfferResult = await InitTradeWithTradeItems(userId, tradeItemIds, 10, 10);

        var commandStub = new RespondTradeCommand
        {
            TradeId = tradeOfferResult.TradeId,
            UserId = userId
        };

        // Act

        var result = await _sut.RejectTradeOfferAsync(commandStub);

        // Assert

        Assert.False(result.Success, "Result should fail");
        Assert.Single(result.Errors);
        Assert.NotEmpty(result.Errors.FirstOrDefault()!);
    }

    [Theory(DisplayName = "Cancel trade")]
    [InlineData("1")]
    [InlineData("1", "2", "3")]
    [InlineData("1", "2", "3", "4", "5")]
    public async Task CancelTradeOffer_CreateTradeOfferThenCancelTradeOffer_ReturnsCancelledTradeOffer(params string[] tradeItemIds)
    {
        // Arrange

        var userId = User.GenerateId();

        var tradeOfferResult = await InitTradeWithTradeItems(userId, tradeItemIds, 10, 10);

        var commandStub = new CancelTradeCommand
        {
            TradeId = tradeOfferResult.TradeId,
            UserId = userId
        };

        // Act

        var result = await _sut.CancelTradeOfferAsync(commandStub);

        // Assert

        Assert.True(result.Success, "Result should be successful");
        Assert.Equal(tradeOfferResult.TradeId, result.TradeId);
        Assert.Equal(defaultUserName, result.ReceiverName);
        Assert.Equal(defaultUserName, result.SenderName);
        Assert.Equal(userId, result.SenderId);
        Assert.Equal(receiverUserId, result.ReceiverId);
        Assert.Null(result.Response);
        Assert.Null(result.ResponseDate);
        Assert.All(result.Items, (item) =>
        {
            Assert.Contains(tradeOfferResult.Items, x =>  x.ItemId == item.ItemId &&
                // x.ItemName == item.ItemName && 
                x.Quantity == item.Quantity &&
                x.Price == item.Price
            );
        });
    }

    [Fact]
    public async Task CancelTradeOffer_CreateTradeOfferCancelTradeOfferThenCancelAgain_ShouldFail()
    {
        // Arrange

        var userId = User.GenerateId();

        var tradeItemIds = new string[] { "1", "2", "3" };

        var tradeOfferResult = await InitTradeWithTradeItems(userId, tradeItemIds, 10, 10);

        var commandStub = new CancelTradeCommand
        {
            TradeId = tradeOfferResult.TradeId,
            UserId = userId
        };

        // Act

        await _sut.CancelTradeOfferAsync(commandStub);
        var result = await _sut.CancelTradeOfferAsync(commandStub);

        // Assert

        // Assert that all locked items were unlocked

        // Assert that all trade items were unlocked
        foreach (var tradeItem in tradeOfferResult.Items)
        {
            _sender.Verify(x => x.Send(It.Is<UnlockItemCommand>(y =>
                y.ItemId == tradeItem.ItemId &&
                y.Quantity == tradeItem.Quantity &&
                y.UserId == userId &&
                y.Notify
            ), It.IsAny<CancellationToken>()), Times.AtLeastOnce());
        }
        Assert.False(result.Success, "Result should fail");
        Assert.Single(result.Errors);
        Assert.NotEmpty(result.Errors.FirstOrDefault()!);
    }

    [Fact]
    public async Task CancelTradeOffer_CreateTradeThenTryToCancelTradeOfferAsReceiver_ShouldFail()
    {
        // Arrange

        var userId = User.GenerateId();
        var tradeItemIds = new string[] { "1" };

        var tradeOfferResult = await InitTradeWithTradeItems(userId, tradeItemIds, 10, 10);

        var commandStub = new CancelTradeCommand
        {
            TradeId = tradeOfferResult.TradeId,
            UserId = receiverUserId
        };

        // Act

        var result = await _sut.CancelTradeOfferAsync(commandStub);

        // Assert

        Assert.False(result.Success, "Result should fail");
        Assert.Single(result.Errors);
        Assert.NotEmpty(result.Errors.FirstOrDefault()!);
    }

    #region Utils

    private async Task<TradeOfferResult> InitTrade(CreateTradeOfferCommand model)
    {
        var trade = await _sut.CreateTradeOfferAsync(model);

        if (model.Items is not null && model.Items.Any())
        {
            currentTradeItems.Add(trade.TradeId, model.Items.Select(x => new TradeItem(trade.TradeId, x.ItemId, x.Quantity, x.Price)).ToList());

            var cachedTrade = cachedTrades.FirstOrDefault(x => x.TradeId == trade.TradeId);

            if (cachedTrade is null)
            {
                
            }

            cachedTrade!.TradeItems = currentTradeItems[trade.TradeId].ToArray();
        }

        return trade;
    }

    private async Task<List<string>> InitTradeOffersAndReturnIds(string senderUserId, int numberOfTradeOffers, string[] tradeItemIds, bool responded = false)
    {
        List<string> tradeOfferIds = new();

        for (int i = 0; i < numberOfTradeOffers; i++)
        {
            var tradeOfferResult = await InitTradeWithTradeItems(senderUserId, tradeItemIds, 10, 10);

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

    private async Task<List<string>> InitTradeOffersAndReturnIds(string senderUserId, string receiverUserId, int numberOfTradeOffers, string[] tradeItemIds, bool responded = false)
    {
        List<string> tradeOfferIds = new();

        for (int i = 0; i < numberOfTradeOffers; i++)
        {
            var tradeOfferResult = await InitTradeWithTradeItems(senderUserId, receiverUserId, tradeItemIds, 10, 10);

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

    private async Task<List<string>> InitTradeOffersAndReturnIds(int numberOfTradeOffers, string[] tradeItemIds, bool responded = false)
    {
        List<string> tradeOfferIds = new();
        var senderUserId = User.GenerateId();

        for (int i = 0; i < numberOfTradeOffers; i++)
        {
            var tradeOfferResult = await InitTradeWithTradeItems(senderUserId, tradeItemIds, 10, 10);

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

    private async Task<TradeOfferResult> InitTradeWithTradeItems(string senderId, string receiverUserId, string[] tradeItemIds, int quantity, int price)
    {
        var inventory = GetInventory(senderId);

        var expectedItemsCount = tradeItemIds.Length;

        var tradeItemDTOs = new TradeItemDTO[expectedItemsCount];

        for (int i = 0; i < expectedItemsCount; i++)
        {
            var itemId = tradeItemIds[i];

            inventory.AddItem(itemId, quantity);

            tradeItemDTOs[i] = new TradeItemDTO
            {
                ItemId = itemId,
                ItemName = GetItemName(itemId),
                Quantity = quantity,
                Price = price
            };
        }

        var createTradeStub = new CreateTradeOfferCommand
        {
            SenderUserId = senderId,
            TargetUserId = receiverUserId,
            Items = tradeItemDTOs
        };

        return await InitTrade(createTradeStub);
    }

    private async Task<TradeOfferResult> InitTradeWithTradeItems(string senderId, string[] tradeItemIds, int quantity, int price)
    {
        var inventory = GetInventory(senderId);

        var expectedItemsCount = tradeItemIds.Length;

        var tradeItemDTOs = new TradeItemDTO[expectedItemsCount];

        for (int i = 0; i < expectedItemsCount; i++)
        {
            var itemId = tradeItemIds[i];

            inventory.AddItem(itemId, quantity);

            tradeItemDTOs[i] = new TradeItemDTO
            {
                ItemId = itemId,
                ItemName = GetItemName(itemId),
                Quantity = quantity,
                Price = price
            };
        }

        var createTradeStub = new CreateTradeOfferCommand
        {
            SenderUserId = senderId,
            TargetUserId = receiverUserId,
            Items = tradeItemDTOs
        };

        return await InitTrade(createTradeStub);
    }

    private Inventory GetInventory(string userId)
    {
        if (!inventories.TryGetValue(userId, out var inventory))
        {
            inventory = new Inventory(userId);

            inventories.Add(userId, inventory);
        }

        return inventory;
    }

    private bool HasInventory(string userId)
    {
        return inventories.ContainsKey(userId);
    }

    private TradeItem[] GetTradeItems(string tradeId) => currentTradeItems[tradeId].ToArray();

    private CachedTrade? GetCachedTrade(string tradeId) => cachedTrades.FirstOrDefault(x => x.TradeId == tradeId);

    private Trade? GetTrade(string tradeId) => collection.FirstOrDefault(x => x.TradeId == tradeId);

    private static string GetItemName(string itemId) => $"item_name_{itemId}";

    #endregion Utils
}
