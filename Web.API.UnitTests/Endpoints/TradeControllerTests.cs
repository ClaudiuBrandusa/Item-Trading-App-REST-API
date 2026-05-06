using Application.Behaviors.Trade.CancelTrade;
using Application.Behaviors.Trade.CreateTrade;
using Application.Behaviors.Trade.GetTrade;
using Application.Behaviors.Trade.ListTrades;
using Application.Behaviors.Trade.RespondTrade;
using Application.Models.Common;
using Application.Models.TradeItems;
using Application.Models.Trades;
using Application.Results.Trades;
using CommonTestUtils.Assertions;
using CommonTestUtils.Extensions;
using Domain.Entities.Identity;
using Item_Trading_App_Contracts.Base.Item;
using Item_Trading_App_Contracts.Requests.Trade;
using Item_Trading_App_Contracts.Responses.Base;
using Item_Trading_App_Contracts.Responses.Trade;
using Item_Trading_App_REST_API.Controllers;
using Item_Trading_App_REST_API.MappingConfigs;
using MapsterMapper;
using MediatR;
using Moq;

namespace Web.API.UnitTests.Endpoints;

public class TradeMappingFixture
{
    public Mapper Mapper { get; }

    public TradeMappingFixture()
    {
        Mapper = new Mapper();

        var itemMappingConfig = new TradeMappingConfig();
        itemMappingConfig.Register(Mapper.Config);
        var generalMappingConfig = new GeneralMappingConfig();
        generalMappingConfig.Register(Mapper.Config);
    }
}

public class TradeControllerTests : IClassFixture<TradeMappingFixture>
{
    private readonly Mapper _mapper;
    
    public TradeControllerTests(TradeMappingFixture fixture)
    {
        _mapper = fixture.Mapper;
    }

    [Fact]
    public async Task Get_RetrieveTradeById_RetrievesSuccessfully()
    {
        // Arrange

        var mediatorMock = new Mock<IMediator>();

        var expectedTradeOfferResult = new TradeOfferResult
        {
            TradeId = "trade-id",
            SenderId = "sender-id",
            SenderName = "sender-name",
            ReceiverId = "receiver-id",
            ReceiverName = "receiver-name",
            CreationDate = DateTime.UtcNow,
            Items = [ new TradeItemDTO { ItemId = "item-id", ItemName = "item-name", Quantity = 10, Price = 10 } ]
        };

        mediatorMock.Setup(x => x.Send(It.IsAny<RequestTradeOfferQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RequestTradeOfferQuery query, CancellationToken ct) =>
                Result<TradeOfferResult>.Success(expectedTradeOfferResult)
            );

        var mediator = mediatorMock.Object;

        var sut = new TradeController(_mapper, mediator);

        // Act

        var result = await sut.Get(expectedTradeOfferResult.TradeId);

        // Assert

        var response = HttpResultAssert.AssertActionResultAsResponse<TradeOfferSuccessResponse>(result);
        Assert.Equal(expectedTradeOfferResult.TradeId, response.TradeId);
        Assert.Equal(expectedTradeOfferResult.SenderId, response.SenderId);
        Assert.Equal(expectedTradeOfferResult.SenderName, response.SenderName);
        Assert.Equal(expectedTradeOfferResult.ReceiverId, response.ReceiverId);
        Assert.Equal(expectedTradeOfferResult.ReceiverName, response.ReceiverName);
        Assert.Equal(expectedTradeOfferResult.CreationDate, response.CreationDate);
        Assert.Equal(expectedTradeOfferResult.ResponseDate, response.ResponseDate);
        Assert.Equal(expectedTradeOfferResult.Response, response.Response);
        Assert.All(expectedTradeOfferResult.Items, item => response.Items.Any(x =>
            x.Id == item.ItemId &&
            x.Name == item.ItemName &&
            x.Price == item.Price &&
            x.Quantity == item.Quantity)
        );
    }

    [Fact]
    public async Task Get_AttemptRetrievingTradeById_ShouldFail()
    {
        // Arrange

        var mediatorMock = new Mock<IMediator>();

        mediatorMock.Setup(x => x.Send(It.IsAny<RequestTradeOfferQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RequestTradeOfferQuery query, CancellationToken ct) =>
                Result<TradeOfferResult>.Failure("Something went wrong")
            );

        var mediator = mediatorMock.Object;

        var sut = new TradeController(_mapper, mediator);

        // Act

        var result = await sut.Get("trade-id");

        // Assert

        var response = HttpResultAssert.AssertActionResultAsFailedResponse<TradeOfferFailedResponse>(result);
        Assert.NotEmpty(response.Errors);
    }

    [Theory]
    [InlineData(TradeDirection.All, true)]
    [InlineData(TradeDirection.All, false)]
    [InlineData(TradeDirection.Sent, true)]
    [InlineData(TradeDirection.Sent, false)]
    [InlineData(TradeDirection.Received, true)]
    [InlineData(TradeDirection.Received, false)]
    public async Task List_ListTrades_RetrievesSuccessfully(TradeDirection direction, bool responded)
    {
        // Arrange

        var expectedTradeItemIds = new string[] { "item-id-0", "item-id-1" };

        var expectedTradeOffersResult = new TradeOffersResult
        {
            SentTradeOfferIds = [ "trade-id" ],
            ReceivedTradeOfferIds = [ "trade-id" ]
        };

        var mediatorMock = new Mock<IMediator>();

        mediatorMock.Setup(x => x.Send(It.IsAny<ListTradesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ListTradesQuery query, CancellationToken ct) =>
                Result<TradeOffersResult>.Success(expectedTradeOffersResult)
            );

        var mediator = mediatorMock.Object;

        var user = new User
        {
            Id = User.GenerateId(),
            UserName = string.Empty
        };

        var sut = new TradeController(_mapper, mediator);

        sut.SetSenderUser(user);

        // Act

        var result = await sut.List(expectedTradeItemIds, direction.ToString(), responded);

        // Assert

        var response = HttpResultAssert.AssertActionResultAsResponse<ListTradeOffersSuccessResponse>(result);
        if (direction == TradeDirection.All || direction == TradeDirection.Sent)
        {
            Assert.NotEmpty(response.SentTradeOfferIds);
        }
        if (direction == TradeDirection.All || direction == TradeDirection.Received)
        {
            Assert.NotEmpty(response.ReceivedTradeOfferIds);
        }
    }

    [Fact]
    public async Task List_AttemptToListTradesWithInvalidDirection_ShouldFail()
    {
        // Arrange

        var mediatorMock = new Mock<IMediator>();

        var mediator = mediatorMock.Object;

        var user = new User
        {
            Id = User.GenerateId(),
            UserName = string.Empty
        };

        var sut = new TradeController(_mapper, mediator);

        sut.SetSenderUser(user);

        // Act

        var result = await sut.List(Array.Empty<string>(), "invalid-direction", false);

        // Assert

        var response = HttpResultAssert.AssertActionResultAsFailedResponse<FailedResponse>(result);
        Assert.NotEmpty(response.Errors);
    }

    [Fact]
    public async Task List_AttemptToListTrades_ShouldFail()
    {
        // Arrange

        var mediatorMock = new Mock<IMediator>();

        mediatorMock.Setup(x => x.Send(It.IsAny<ListTradesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ListTradesQuery query, CancellationToken ct) =>
                Result<TradeOffersResult>.Failure("Something went wrong")
            );

        var mediator = mediatorMock.Object;

        var user = new User
        {
            Id = User.GenerateId(),
            UserName = string.Empty
        };

        var sut = new TradeController(_mapper, mediator);

        sut.SetSenderUser(user);

        // Act

        var result = await sut.List(Array.Empty<string>(), TradeDirection.All.ToString(), true);

        // Assert

        var response = HttpResultAssert.AssertActionResultAsFailedResponse<FailedResponse>(result);
        Assert.NotEmpty(response.Errors);
    }

    [Fact]
    public async Task GetTradeDirections_RetrievesListOfTradeDirections_RetrievesSuccessfully()
    {
        // Arrange

        var mediatorMock = new Mock<IMediator>();

        var mediator = mediatorMock.Object;

        var sut = new TradeController(_mapper, mediator);

        // Act

        var result = await sut.GetTradeDirections();

        // Assert

        var response = HttpResultAssert.AssertActionResultAsResponse<string[]>(result);
        Assert.NotEmpty(response);
    }

    [Fact]
    public async Task Offer_CreateNewTradeOffer_RetrievesCreatedTradeOffer()
    {
        // Arrange

        var expectedTradeOfferResult = new TradeOfferResult
        {
            TradeId = "trade-id",
            SenderId = "sender-id",
            SenderName = "SenderName",
            ReceiverId = "receiver-id",
            ReceiverName = "ReceiverName",
            CreationDate = DateTime.UtcNow,
            Items = [
                new TradeItemDTO
                {
                    ItemId = "item-id",
                    ItemName = "itemName",
                    Price = 10,
                    Quantity = 10
                }
            ]
        };

        var mediatorMock = new Mock<IMediator>();

        mediatorMock.Setup(x => x.Send(It.IsAny<CreateTradeOfferCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CreateTradeOfferCommand request, CancellationToken ct) =>
                Result<TradeOfferResult>.Success(expectedTradeOfferResult)
            );

        var mediator = mediatorMock.Object;

        var user = new User
        {
            Id = User.GenerateId(),
            UserName = string.Empty
        };

        var sut = new TradeController(_mapper, mediator);

        sut.SetSenderUser(user);

        var request = new TradeOfferRequest
        {
            TargetUserId = "user-id",
            Items = [new ItemWithPrice()]
        };

        // Act

        var result = await sut.Offer(request);

        // Assert

        var response = HttpResultAssert.AssertActionResultAsResponse<TradeOfferSuccessResponse>(result);
        Assert.Equal(expectedTradeOfferResult.TradeId, response.TradeId);
        Assert.Equal(expectedTradeOfferResult.SenderId, response.SenderId);
        Assert.Equal(expectedTradeOfferResult.SenderName, response.SenderName);
        Assert.Equal(expectedTradeOfferResult.ReceiverId, response.ReceiverId);
        Assert.Equal(expectedTradeOfferResult.ReceiverName, response.ReceiverName);
        Assert.Equal(expectedTradeOfferResult.CreationDate, response.CreationDate);
        Assert.Equal(expectedTradeOfferResult.ResponseDate, response.ResponseDate);
        Assert.Equal(expectedTradeOfferResult.Response, response.Response);
        Assert.All(expectedTradeOfferResult.Items, item =>
            response.Items.Any(x =>
                x.Id == item.ItemId &&
                x.Name == item.ItemName &&
                x.Price == item.Price &&
                x.Quantity == item.Quantity
            )
        );
    }

    [Fact]
    public async Task Offer_AttemptCreatingNewTradeOffer_ShouldFail()
    {
        // Arrange

        var mediatorMock = new Mock<IMediator>();

        mediatorMock.Setup(x => x.Send(It.IsAny<CreateTradeOfferCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CreateTradeOfferCommand request, CancellationToken ct) =>
                Result<TradeOfferResult>.Failure("Something went wrong")
            );
        
        var mediator = mediatorMock.Object;

        var user = new User
        {
            Id = User.GenerateId(),
            UserName = string.Empty
        };

        var sut = new TradeController(_mapper, mediator);

        sut.SetSenderUser(user);

        var request = new TradeOfferRequest
        {
            TargetUserId = "user-id",
            Items = [new ItemWithPrice()]
        };

        // Act

        var result = await sut.Offer(request);

        // Assert

        var response = HttpResultAssert.AssertActionResultAsFailedResponse<TradeOfferFailedResponse>(result);
        Assert.NotEmpty(response.Errors);
    }

    [Fact]
    public async Task Accept_AcceptTradeOffer_AcceptsSuccessfully()
    {
        // Arrange

        var expectedTradeOfferResult = new TradeOfferResult
        {
            TradeId = "trade-id",
            SenderId = "sender-id",
            SenderName = "SenderName",
            ReceiverId = "receiver-id",
            ReceiverName = "ReceiverName",
            CreationDate = DateTime.UtcNow,
            ResponseDate = DateTime.UtcNow,
            Response = true,
            Items = [
                new TradeItemDTO
                {
                    ItemId = "item-id",
                    ItemName = "itemName",
                    Price = 10,
                    Quantity = 10
                }
            ]
        };

        var mediatorMock = new Mock<IMediator>();

        mediatorMock.Setup(x => x.Send(It.IsAny<RespondTradeCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RespondTradeCommand request, CancellationToken ct) =>
                Result<TradeOfferResult>.Success(expectedTradeOfferResult)
            );

        var mediator = mediatorMock.Object;

        var user = new User
        {
            Id = User.GenerateId(),
            UserName = string.Empty
        };

        var sut = new TradeController(_mapper, mediator);

        sut.SetSenderUser(user);

        var request = new AcceptTradeOfferRequest
        {
            TradeId = "trade-id"
        };

        // Act

        var result = await sut.Accept(request);

        // Assert

        var response = HttpResultAssert.AssertActionResultAsResponse<AcceptTradeOfferSuccessResponse>(result);
        Assert.Equal(expectedTradeOfferResult.TradeId, response.TradeId);
        Assert.Equal(expectedTradeOfferResult.SenderId, response.SenderId);
        Assert.Equal(expectedTradeOfferResult.SenderName, response.SenderName);
        Assert.Equal(expectedTradeOfferResult.CreationDate, response.CreationDate);
        Assert.Equal(expectedTradeOfferResult.ResponseDate, response.ResponseDate);
    }

    [Fact]
    public async Task Accept_AttemptAcceptingTradeOffer_ShouldFail()
    {
        // Arrange

        var mediatorMock = new Mock<IMediator>();

        mediatorMock.Setup(x => x.Send(It.IsAny<RespondTradeCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RespondTradeCommand request, CancellationToken ct) =>
                Result<TradeOfferResult>.Failure("Something went wrong")
            );

        var mediator = mediatorMock.Object;

        var user = new User
        {
            Id = User.GenerateId(),
            UserName = string.Empty
        };

        var sut = new TradeController(_mapper, mediator);

        sut.SetSenderUser(user);

        var request = new AcceptTradeOfferRequest
        {
            TradeId = "trade-id"
        };

        // Act

        var result = await sut.Accept(request);

        // Assert

        var response = HttpResultAssert.AssertActionResultAsFailedResponse<AcceptTradeOfferFailedResponse>(result);
        Assert.NotEmpty(response.Errors);
    }

    [Fact]
    public async Task Reject_RejectTradeOffer_RejectsSuccessfully()
    {
        // Arrange

        var expectedTradeOfferResult = new TradeOfferResult
        {
            TradeId = "trade-id",
            SenderId = "sender-id",
            SenderName = "SenderName",
            ReceiverId = "receiver-id",
            ReceiverName = "ReceiverName",
            CreationDate = DateTime.UtcNow,
            ResponseDate = DateTime.UtcNow,
            Response = false,
            Items = [
                new TradeItemDTO
                {
                    ItemId = "item-id",
                    ItemName = "itemName",
                    Price = 10,
                    Quantity = 10
                }
            ]
        };

        var mediatorMock = new Mock<IMediator>();

        mediatorMock.Setup(x => x.Send(It.IsAny<RespondTradeCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RespondTradeCommand request, CancellationToken ct) =>
                Result<TradeOfferResult>.Success(expectedTradeOfferResult)
            );

        mediatorMock.Setup(x => x.Send(It.IsAny<RespondTradeCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RespondTradeCommand request, CancellationToken ct) =>
                Result<TradeOfferResult>.Success(expectedTradeOfferResult)
            );

        var mediator = mediatorMock.Object;

        var user = new User
        {
            Id = User.GenerateId(),
            UserName = string.Empty
        };

        var sut = new TradeController(_mapper, mediator);

        sut.SetSenderUser(user);

        var request = new RejectTradeOfferRequest
        {
            TradeId = "trade-id"
        };

        // Act

        var result = await sut.Reject(request);

        // Assert

        var response = HttpResultAssert.AssertActionResultAsResponse<RejectTradeOfferSuccessResponse>(result);
        Assert.Equal(expectedTradeOfferResult.TradeId, response.TradeId);
        Assert.Equal(expectedTradeOfferResult.SenderId, response.SenderId);
        Assert.Equal(expectedTradeOfferResult.SenderName, response.SenderName);
        Assert.Equal(expectedTradeOfferResult.CreationDate, response.CreationDate);
        Assert.Equal(expectedTradeOfferResult.ResponseDate, response.ResponseDate);
    }

    [Fact]
    public async Task Reject_AttemptRejectingTradeOffer_ShouldFail()
    {
        // Arrange

        var mediatorMock = new Mock<IMediator>();

        mediatorMock.Setup(x => x.Send(It.IsAny<RespondTradeCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RespondTradeCommand request, CancellationToken ct) =>
                Result<TradeOfferResult>.Failure("Something went wrong")
            );
        
        var mediator = mediatorMock.Object;

        var user = new User
        {
            Id = User.GenerateId(),
            UserName = string.Empty
        };

        var sut = new TradeController(_mapper, mediator);

        sut.SetSenderUser(user);

        var request = new RejectTradeOfferRequest
        {
            TradeId = "trade-id"
        };

        // Act

        var result = await sut.Reject(request);

        // Assert

        var response = HttpResultAssert.AssertActionResultAsFailedResponse<RejectTradeOfferFailedResponse>(result);
        Assert.NotEmpty(response.Errors);
    }

    [Fact]
    public async Task Cancel_CancelTradeOffer_CancelsSuccessfully()
    {
        // Arrange

        var expectedTradeOfferResult = new TradeOfferResult
        {
            TradeId = "trade-id",
            SenderId = "sender-id",
            SenderName = "SenderName",
            ReceiverId = "receiver-id",
            ReceiverName = "ReceiverName",
            CreationDate = DateTime.UtcNow,
            ResponseDate = DateTime.UtcNow,
            Items = [
                new TradeItemDTO
                {
                    ItemId = "item-id",
                    ItemName = "itemName",
                    Price = 10,
                    Quantity = 10
                }
            ]
        };

        var mediatorMock = new Mock<IMediator>();

        mediatorMock.Setup(x => x.Send(It.IsAny<CancelTradeCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CancelTradeCommand request, CancellationToken ct) =>
                Result<TradeOfferResult>.Success(expectedTradeOfferResult)
            );
        
        var mediator = mediatorMock.Object;

        var user = new User
        {
            Id = User.GenerateId(),
            UserName = string.Empty
        };

        var sut = new TradeController(_mapper, mediator);

        sut.SetSenderUser(user);

        var request = new CancelTradeOfferRequest
        {
            TradeId = "trade-id"
        };

        // Act

        var result = await sut.Cancel(request);

        // Assert

        var response = HttpResultAssert.AssertActionResultAsResponse<CancelTradeOfferSuccessResponse>(result);
        Assert.Equal(expectedTradeOfferResult.TradeId, response.TradeId);
        Assert.Equal(expectedTradeOfferResult.ReceiverId, response.ReceiverId);
        Assert.Equal(expectedTradeOfferResult.ReceiverName, response.ReceiverName);
    }

    [Fact]
    public async Task Cancel_AttemptCancelingTradeOffer_ShouldFail()
    {
        // Arrange

        var mediatorMock = new Mock<IMediator>();

        mediatorMock.Setup(x => x.Send(It.IsAny<CancelTradeCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CancelTradeCommand request, CancellationToken ct) =>
                Result<TradeOfferResult>.Failure("Something went wrong")
            );
        
        var mediator = mediatorMock.Object;

        var user = new User
        {
            Id = User.GenerateId(),
            UserName = string.Empty
        };

        var sut = new TradeController(_mapper, mediator);

        sut.SetSenderUser(user);

        var request = new CancelTradeOfferRequest
        {
            TradeId = "trade-id"
        };

        // Act

        var result = await sut.Cancel(request);

        // Assert

        var response = HttpResultAssert.AssertActionResultAsFailedResponse<CancelTradeOfferFailedResponse>(result);
        Assert.NotEmpty(response.Errors);
    }
}
