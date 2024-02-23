using Item_Trading_App_REST_API.Entities;
using Item_Trading_App_REST_API.Models.Inventory;
using Item_Trading_App_REST_API.Models.Item;
using Item_Trading_App_REST_API.Models.Trade;
using Item_Trading_App_REST_API.Models.TradeItemHistory;
using Item_Trading_App_REST_API.Models.TradeItems;
using Item_Trading_App_REST_API.Resources.Commands.Inventory;
using Item_Trading_App_REST_API.Resources.Commands.Trade;
using Item_Trading_App_REST_API.Resources.Commands.TradeItem;
using Item_Trading_App_REST_API.Resources.Commands.TradeItemHistory;
using Item_Trading_App_REST_API.Resources.Queries.Identity;
using Item_Trading_App_REST_API.Resources.Queries.Inventory;
using Item_Trading_App_REST_API.Resources.Queries.Item;
using Item_Trading_App_REST_API.Resources.Queries.Trade;
using Item_Trading_App_REST_API.Resources.Queries.TradeItem;
using Item_Trading_App_REST_API.Resources.Queries.TradeItemHistory;
using Item_Trading_App_REST_API.Services.Cache;
using Item_Trading_App_REST_API.Services.Trade;
using Item_Trading_App_REST_API.Services.UnitOfWork;
using MediatR;

namespace Item_Trading_App_Tests;

public class TradeTests
{
    private readonly ITradeService _sut; // service under test
    private readonly string senderUserId = Guid.NewGuid().ToString();
    private readonly string receiverUserId = Guid.NewGuid().ToString();
    private readonly string defaultUserName = "default_username";
    private readonly Dictionary<string, List<TradeItem>> currentTradeItems = new();

    public TradeTests()
    {
        var _context = TestingUtils.GetDatabaseContext();
        var _mapper = TestingUtils.GetMapper();
        var mediatorMock = new Mock<IMediator>();
        var cacheServiceMock = new Mock<ICacheService>();
        var unitOfWorkMock = new Mock<IUnitOfWorkService>();

        #region MediatorMocks

        mediatorMock.Setup(x => x.Send(It.IsAny<IRequest<bool>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IRequest<bool> request, CancellationToken ct) =>
            {
                return true;
            });
        mediatorMock.Setup(x => x.Send(It.IsAny<IRequest<LockItemResult>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IRequest<LockItemResult> request, CancellationToken ct) =>
            {
                return new LockItemResult
                {
                    Success = true
                };
            });
        mediatorMock.Setup(x => x.Send(It.IsAny<GetItemNameQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetItemNameQuery request, CancellationToken ct) =>
            {
                return TestingData.GetTradeItemName(request.ItemId);
            });
        mediatorMock.Setup(x => x.Send(It.IsAny<GetUsernameQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetUsernameQuery request, CancellationToken ct) =>
            {
                return defaultUserName;
            });
        mediatorMock.Setup(x => x.Send(It.IsAny<IRequest<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IRequest<int> request, CancellationToken ct) =>
            {
                return 500;
            });
        mediatorMock.Setup(x => x.Send(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
            .Callback((IRequest request, CancellationToken ct) =>
            {
                var tradeContent = _mapper.From((AddTradeItemCommand)request).AdaptToType<TradeContent>();
                _context.TradeContent.Add(tradeContent);
            });
        mediatorMock.Setup(x => x.Send(It.IsAny<GetTradeItemsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IRequest<TradeItem[]> request, CancellationToken ct) =>
            {
                return currentTradeItems[((GetTradeItemsQuery)request).TradeId].Select(x => new TradeItem { ItemId = x.ItemId, Name = "", Price = 0, Quantity = x.Quantity}).ToArray();
            });
        mediatorMock.Setup(x => x.Send(It.IsAny<GetTradeItemsHistoryQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IRequest<TradeItem[]> request, CancellationToken ct) =>
            {
                return currentTradeItems[((GetTradeItemsHistoryQuery)request).TradeId].Select(x => new TradeItem { ItemId = x.ItemId, Name = "", Price = 0, Quantity = x.Quantity }).ToArray();
            });
        mediatorMock.Setup(x => x.Send(It.IsAny<AddTradeItemsHistoryCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IRequest<TradeItemHistoryBaseResult> request, CancellationToken ct) =>
            {
                return new TradeItemHistoryBaseResult { Success = true };
            });
        mediatorMock.Setup(x => x.Send(It.IsAny<RemoveTradeItemsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IRequest<bool> request, CancellationToken ct) =>
            {
                return true;
            });
        mediatorMock.Setup(x => x.Send(It.IsAny<HasItemQuantityQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IRequest<bool> request, CancellationToken ct) =>
            {
                return true;
            });
        mediatorMock.Setup(x => x.Send(It.IsAny<AddInventoryItemCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IRequest<QuantifiedItemResult> request, CancellationToken ct) =>
             {
                 return new QuantifiedItemResult
                 {
                     Success = true
                 };
             });
        mediatorMock.Setup(x => x.Send(It.IsAny<DropInventoryItemCommand>(), It.IsAny<CancellationToken>()))
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

        _sut = new TradeService(TestingUtils.GetDatabaseContextWrapper(Guid.NewGuid().ToString()), cacheServiceMock.Object, mediatorMock.Object, _mapper, unitOfWorkMock.Object);
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
            TargetUserId = receiverUserId
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

    private async Task<TradeOfferResult> InitTrade(CreateTradeOfferCommand model)
    {
        var trade = await _sut.CreateTradeOfferAsync(model);

        if (model.Items is not null && model.Items.Any())
            currentTradeItems.Add(trade.TradeId, model.Items.Select(x => new TradeItem { ItemId = x.ItemId, Quantity = x.Quantity }).ToList());

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
}
