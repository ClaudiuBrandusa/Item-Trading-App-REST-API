using Item_Trading_App_REST_API.Entities;
using Item_Trading_App_REST_API.Models.Inventory;
using Item_Trading_App_REST_API.Models.Item;
using Item_Trading_App_REST_API.Models.Trade;
using Item_Trading_App_REST_API.Models.TradeItems;
using Item_Trading_App_REST_API.Resources.Commands.Trade;
using Item_Trading_App_REST_API.Resources.Commands.TradeItem;
using Item_Trading_App_REST_API.Resources.Queries.Trade;
using Item_Trading_App_REST_API.Resources.Queries.TradeItem;
using Item_Trading_App_REST_API.Services.Cache;
using Item_Trading_App_REST_API.Services.Notification;
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
        mediatorMock.Setup(x => x.Send(It.IsAny<IRequest<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IRequest<string> request, CancellationToken ct) =>
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
        mediatorMock.Setup(x => x.Send(It.IsAny<IRequest<List<TradeItem>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IRequest<List<TradeItem>> request, CancellationToken ct) =>
            {
                return currentTradeItems[((GetTradeItemsQuery)request).TradeId];
            });
        mediatorMock.Setup(x => x.Send(It.IsAny<IRequest<List<ItemPrice>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IRequest<List<ItemPrice>> request, CancellationToken ct) =>
            {
                return currentTradeItems[((GetItemPricesQuery)request).TradeId].Select(x => new ItemPrice { ItemId = x.ItemId, Name = "", Price = 0, Quantity = x.Quantity}).ToList();
            });

        var cacheServiceMock = new Mock<ICacheService>();
        cacheServiceMock.Setup(x => x.ListWithPrefix<ItemPrice>(It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync((string prefix, bool removePrefix) =>
            {
                return new Dictionary<string, ItemPrice>();
            });
        cacheServiceMock.Setup(x => x.ListWithPrefix<string>(It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync((string prefix, bool removePrefix) =>
            {
                return new Dictionary<string, string>();
            });
        var notificationServiceMock = new Mock<INotificationService>();

        var unitOfWorkMock = new Mock<IUnitOfWorkService>();

        _sut = new TradeService(_context, cacheServiceMock.Object, mediatorMock.Object, notificationServiceMock.Object, _mapper, unitOfWorkMock.Object);
    }

    [Theory(DisplayName = "Create trade offer")]
    [InlineData("1")]
    [InlineData("1", "2", "3")]
    [InlineData("1", "2", "3", "4", "5")]
    public async void CreateTrade(params string[] itemPriceIds)
    {
        var itemPrices = TestingData.GetItemPrices(itemPriceIds);

        for(int i = 0; i < itemPriceIds.Length; i++)
        {
            itemPrices[i].Price += i;
            itemPrices[i].Quantity += i * 2;
        }

        var result = await InitTrade(new CreateTradeOfferCommand
        {
            SenderUserId = senderUserId,
            TargetUserId = receiverUserId,
            Items = itemPrices
        });

        Assert.True(result.Success, "The result should be successful");
        Assert.False(string.IsNullOrEmpty(result.TradeOfferId), "The trade offer id must not be empty or null");
        Assert.True(result.Items.All(x => itemPriceIds.Contains(x.ItemId)), "The trade offer's items should contain all of the inserted items");
        Assert.Equal(defaultUserName, result.ReceiverName);
    }

    [Theory(DisplayName = "Get sent trade")]
    [InlineData("1")]
    [InlineData("1", "2", "3")]
    public async void GetSentTrade(params string[] itemPriceIds)
    {
        var itemPrices = TestingData.GetItemPrices(itemPriceIds);

        for (int i = 0; i < itemPriceIds.Length; i++)
        {
            itemPrices[i].Price += i;
            itemPrices[i].Quantity += i * 2;
        }

        var tradeOfferResult = await InitTrade(new CreateTradeOfferCommand
        {
            SenderUserId = senderUserId,
            TargetUserId = receiverUserId,
            Items = itemPrices
        });

        var result = await _sut.GetSentTradeOffer(new RequestTradeOfferQuery
        {
            TradeOfferId = tradeOfferResult.TradeOfferId,
            UserId = senderUserId
        });

        Assert.True(result.Success, "The result should be successful");
        Assert.Equal(itemPriceIds.Length, result.Items.Count());
        Assert.True(result.Items.All(x => itemPriceIds.Contains(x.ItemId)));
        Assert.Equal(receiverUserId, result.ReceiverId);
    }

    [Theory(DisplayName = "Get sent trades")]
    [InlineData(1, "1")]
    [InlineData(5, "1", "2", "3")]
    public async void GetSentTrades(int numberOfTradeOffers, params string[] itemPriceIds)
    {
        List<string> tradeOfferIds = new();

        for(int i = 0; i < numberOfTradeOffers; i++)
        {
            var itemPrices = TestingData.GetItemPrices(itemPriceIds);

            for (int j = 0; j < itemPriceIds.Length; j++)
            {
                itemPrices[j].Price += j;
                itemPrices[j].Quantity += j * 2;
            }

            var tradeOfferResult = await InitTrade(new CreateTradeOfferCommand
            {
                SenderUserId = senderUserId,
                TargetUserId = receiverUserId,
                Items = itemPrices
            });

            tradeOfferIds.Add(tradeOfferResult.TradeOfferId);
        }

        var result = await _sut.GetSentTradeOffers(new ListTradesQuery { UserId = senderUserId });

        Assert.True(result.Success, "The result should be successful");
        Assert.True(tradeOfferIds.All(x => result.TradeOffers.Contains(x)));
    }

    [Theory(DisplayName = "Get responded sent trades")]
    [InlineData(1, "1")]
    [InlineData(5, "1", "2", "3")]
    public async void GetRespondedSentTrades(int numberOfTradeOffers, params string[] itemPriceIds)
    {
        List<string> tradeOfferIds = new();

        for (int i = 0; i < numberOfTradeOffers; i++)
        {
            var itemPrices = TestingData.GetItemPrices(itemPriceIds);

            for (int j = 0; j < itemPriceIds.Length; j++)
            {
                itemPrices[j].Price += j;
                itemPrices[j].Quantity += j * 2;
            }

            var tradeOfferResult = await InitTrade(new CreateTradeOfferCommand
            {
                SenderUserId = senderUserId,
                TargetUserId = receiverUserId,
                Items = itemPrices
            });

            await _sut.AcceptTradeOffer(new RespondTradeCommand
            {
                TradeId = tradeOfferResult.TradeOfferId,
                UserId = receiverUserId
            });

            tradeOfferIds.Add(tradeOfferResult.TradeOfferId);
        }

        var result = await _sut.GetSentRespondedTradeOffers(new ListTradesQuery { UserId = senderUserId });

        Assert.True(result.Success, "The result should be successful");
        Assert.True(tradeOfferIds.All(x => result.TradeOffers.Contains(x)));
    }

    [Theory(DisplayName = "Get received trade")]
    [InlineData("1")]
    [InlineData("1", "2", "3")]
    public async void GetReceivedTrade(params string[] itemPriceIds)
    {
        var itemPrices = TestingData.GetItemPrices(itemPriceIds);

        for (int i = 0; i < itemPriceIds.Length; i++)
        {
            itemPrices[i].Price += i;
            itemPrices[i].Quantity += i * 2;
        }

        var tradeOfferResult = await InitTrade(new CreateTradeOfferCommand
        {
            SenderUserId = senderUserId,
            TargetUserId = receiverUserId,
            Items = itemPrices
        });

        var result = await _sut.GetReceivedTradeOffer(new RequestTradeOfferQuery
        {
            TradeOfferId = tradeOfferResult.TradeOfferId,
            UserId = receiverUserId
        });

        Assert.True(result.Success, "The result should be successful");
        Assert.Equal(itemPriceIds.Length, result.Items.Count());
        Assert.True(result.Items.All(x => itemPriceIds.Contains(x.ItemId)));
        Assert.Equal(senderUserId, result.SenderId);
    }

    [Theory(DisplayName = "Get received trades")]
    [InlineData(1, "1")]
    [InlineData(5, "1", "2", "3")]
    public async void GetReceivedTrades(int numberOfTradeOffers, params string[] itemPriceIds)
    {
        List<string> tradeOfferIds = new();

        for (int i = 0; i < numberOfTradeOffers; i++)
        {
            var itemPrices = TestingData.GetItemPrices(itemPriceIds);

            for (int j = 0; j < itemPriceIds.Length; j++)
            {
                itemPrices[j].Price += j;
                itemPrices[j].Quantity += j * 2;
            }

            var tradeOfferResult = await InitTrade(new CreateTradeOfferCommand
            {
                SenderUserId = senderUserId,
                TargetUserId = receiverUserId,
                Items = itemPrices
            });

            tradeOfferIds.Add(tradeOfferResult.TradeOfferId);
        }

        var result = await _sut.GetReceivedTradeOffers(new ListTradesQuery { UserId = receiverUserId });

        Assert.True(result.Success, "The result should be successful");
        Assert.True(tradeOfferIds.All(x => result.TradeOffers.Contains(x)));
    }

    [Theory(DisplayName = "Get responded received trades")]
    [InlineData(1, "1")]
    [InlineData(5, "1", "2", "3")]
    public async void GetRespondedReceivedTrades(int numberOfTradeOffers, params string[] itemPriceIds)
    {
        List<string> tradeOfferIds = new();

        for (int i = 0; i < numberOfTradeOffers; i++)
        {
            var itemPrices = TestingData.GetItemPrices(itemPriceIds);

            for (int j = 0; j < itemPriceIds.Length; j++)
            {
                itemPrices[j].Price += j;
                itemPrices[j].Quantity += j * 2;
            }

            var tradeOfferResult = await InitTrade(new CreateTradeOfferCommand
            {
                SenderUserId = senderUserId,
                TargetUserId = receiverUserId,
                Items = itemPrices
            });

            await _sut.AcceptTradeOffer(new RespondTradeCommand
            {
                TradeId = tradeOfferResult.TradeOfferId,
                UserId = receiverUserId
            });

            tradeOfferIds.Add(tradeOfferResult.TradeOfferId);
        }

        var result = await _sut.GetReceivedRespondedTradeOffers(new ListTradesQuery { UserId = receiverUserId });

        Assert.True(result.Success, "The result should be successful");
        Assert.True(tradeOfferIds.All(x => result.TradeOffers.Contains(x)));
    }

    [Theory(DisplayName = "Accept trade")]
    [InlineData("1")]
    [InlineData("1", "2", "3")]
    [InlineData("1", "2", "3", "4", "5")]
    public async void AcceptTradeOffer(params string[] itemPriceIds)
    {
        var itemPrices = TestingData.GetItemPrices(itemPriceIds);

        for (int i = 0; i < itemPriceIds.Length; i++)
        {
            itemPrices[i].Price += i;
            itemPrices[i].Quantity += i * 2;
        }

        var tradeOfferResult = await InitTrade(new CreateTradeOfferCommand
        {
            SenderUserId = senderUserId,
            TargetUserId = receiverUserId,
            Items = itemPrices
        });

        var result = await _sut.AcceptTradeOffer(new RespondTradeCommand
        {
            TradeId = tradeOfferResult.TradeOfferId,
            UserId = receiverUserId
        });

        Assert.True(result.Success, "Result should be successful");
        Assert.Equal(tradeOfferResult.TradeOfferId, result.TradeOfferId);
        Assert.Equal(senderUserId, result.SenderId);
    }

    [Theory(DisplayName = "Reject trade")]
    [InlineData("1")]
    [InlineData("1", "2", "3")]
    [InlineData("1", "2", "3", "4", "5")]
    public async void RejectTradeOffer(params string[] itemPriceIds)
    {
        var itemPrices = TestingData.GetItemPrices(itemPriceIds);

        for (int i = 0; i < itemPriceIds.Length; i++)
        {
            itemPrices[i].Price += i;
            itemPrices[i].Quantity += i * 2;
        }

        var tradeOfferResult = await InitTrade(new CreateTradeOfferCommand
        {
            SenderUserId = senderUserId,
            TargetUserId = receiverUserId,
            Items = itemPrices
        });

        var result = await _sut.RejectTradeOffer(new RespondTradeCommand
        {
            TradeId = tradeOfferResult.TradeOfferId,
            UserId = receiverUserId
        });

        Assert.True(result.Success, "Result should be successful");
        Assert.Equal(tradeOfferResult.TradeOfferId, result.TradeOfferId);
        Assert.Equal(senderUserId, result.SenderId);
    }

    [Theory(DisplayName = "Cancel trade")]
    [InlineData("1")]
    [InlineData("1", "2", "3")]
    [InlineData("1", "2", "3", "4", "5")]
    public async void CancelTradeOffer(params string[] itemPriceIds)
    {
        var itemPrices = TestingData.GetItemPrices(itemPriceIds);

        for (int i = 0; i < itemPriceIds.Length; i++)
        {
            itemPrices[i].Price += i;
            itemPrices[i].Quantity += i * 2;
        }

        var tradeOfferResult = await InitTrade(new CreateTradeOfferCommand
        {
            SenderUserId = senderUserId,
            TargetUserId = receiverUserId,
            Items = itemPrices
        });

        var result = await _sut.CancelTradeOffer(new CancelTradeCommand
        {
            TradeId = tradeOfferResult.TradeOfferId,
            UserId = senderUserId
        });

        Assert.True(result.Success, "Result should be successful");
        Assert.Equal(tradeOfferResult.TradeOfferId, result.TradeOfferId);
        Assert.Equal(receiverUserId, result.ReceiverId);
    }

    private async Task<SentTradeOfferResult> InitTrade(CreateTradeOfferCommand model)
    {
        var trade = await _sut.CreateTradeOffer(model);

        currentTradeItems.Add(trade.TradeOfferId, model.Items.Select(x => new TradeItem { ItemId = x.ItemId, Quantity = x.Quantity }).ToList());

        return trade;
    }
}
