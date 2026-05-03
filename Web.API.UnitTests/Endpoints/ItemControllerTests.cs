using Application.Behaviors.Item.CreateItem;
using Application.Behaviors.Item.DeleteItem;
using Application.Behaviors.Item.GetItem;
using Application.Behaviors.Item.ListItems;
using Application.Behaviors.Item.UpdateItem;
using Application.Behaviors.Trade.GetTradeItemIds;
using Application.Models.Common;
using Application.Results.Items;
using CommonTestUtils.Assertions;
using CommonTestUtils.Extensions;
using Domain.Entities.Identity;
using Item_Trading_App_Contracts.Requests.Item;
using Item_Trading_App_Contracts.Responses.Base;
using Item_Trading_App_Contracts.Responses.Item;
using Item_Trading_App_REST_API.Controllers;
using Item_Trading_App_REST_API.MappingConfigs;
using MapsterMapper;
using MediatR;
using Moq;

namespace Web.API.UnitTests.Endpoints;

public class ItemMappingFixture
{
    public Mapper Mapper { get; }

    public ItemMappingFixture()
    {
        Mapper = new Mapper();

        var itemMappingConfig = new ItemMappingConfig();
        itemMappingConfig.Register(Mapper.Config);
    }
}

public class ItemControllerTests : IClassFixture<ItemMappingFixture>
{
    private readonly Mapper _mapper;

    private readonly string _expectedItemId = "item-id";
    private readonly string _expectedItemName = "item-name";
    private readonly string _expectedItemDescription = "item-description";
    private readonly string _expectedSenderUserId = "user-id";

    public ItemControllerTests(ItemMappingFixture fixture)
    {
        _mapper = fixture.Mapper;
    }

    [Fact]
    public async Task Get_GetItem_ReturnsPropertyItemResponse()
    {
        // Arrange

        var mediatorMock = new Mock<IMediator>();

        mediatorMock.Setup(x => x.Send(It.IsAny<IRequest<Result<FullItemResult>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetItemQuery query, CancellationToken ct) =>
                Result<FullItemResult>.Success(new FullItemResult
                {
                    ItemId = _expectedItemId, 
                    ItemName = _expectedItemName,
                    ItemDescription = _expectedItemDescription
                })
            );

        var mediator = mediatorMock.Object;

        var sut = new ItemController(_mapper, mediator);

        // Act

        var result = await sut.Get(_expectedItemId);

        // Assert

        var response = HttpResultAssert.AssertActionResultAsResponse<ItemResponse>(result);
        Assert.Equal(_expectedItemId, response.Id);
        Assert.Equal(_expectedItemName, response.Name);
        Assert.Equal(_expectedItemDescription, response.Description);
        mediatorMock.Verify(x => x.Send(It.Is<GetItemQuery>(y => y.ItemId == _expectedItemId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Get_GetItemWithInvalidId_ShouldReturnFailedResponse()
    {
        // Arrange

        var mediator = new Mock<IMediator>().Object;

        var sut = new ItemController(_mapper, mediator);

        // Act

        var result = await sut.Get(string.Empty);

        // Assert

        var failedResponse = HttpResultAssert.AssertActionResultAsFailedResponse<FailedResponse>(result);
        Assert.NotEmpty(failedResponse.Errors);
    }

    [Fact]
    public async Task Get_GetItemThatDoesntExist_ShouldReturnFailedResponse()
    {
        // Arrange

        var mediator = new Mock<IMediator>().Object;

        var sut = new ItemController(_mapper, mediator);

        // Act

        var result = await sut.Get("invalid-id");

        // Assert

        var failedResponse = HttpResultAssert.AssertActionResultAsFailedResponse<FailedResponse>(result);
        Assert.NotEmpty(failedResponse.Errors);
    }

    [Fact]
    public async Task List_ListItems_ReturnsListOfItemIds()
    {
        // Arrange

        var mediatorMock = new Mock<IMediator>();

        var expectedItemIds = new string[] { "item-id-0", "item-id-1" };

        mediatorMock.Setup(x => x.Send(It.IsAny<IRequest<Result<ItemsResult>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ListItemsQuery query, CancellationToken ct) =>
                Result<ItemsResult>.Success(new ItemsResult
                {
                    ItemsId = expectedItemIds
                })
            );

        var mediator = mediatorMock.Object;

        var sut = new ItemController(_mapper, mediator);

        // Act

        var result = await sut.List(string.Empty);

        // Assert

        var response = HttpResultAssert.AssertActionResultAsResponse<ItemsResponse>(result);
        Assert.All(response.ItemsId, itemId => response.ItemsId.Contains(itemId));
    }

    [Fact]
    public async Task List_ListItemsWithSearchstring_ReturnsListOfItemIds()
    {
        // Arrange

        var mediatorMock = new Mock<IMediator>();

        var expectedSearchstring = "item";

        var expectedItemIds = new string[] { "item-id-0", "item-id-1" };

        mediatorMock.Setup(x => x.Send(It.IsAny<IRequest<Result<ItemsResult>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ListItemsQuery query, CancellationToken ct) =>
                Result<ItemsResult>.Success(new ItemsResult
                {
                    ItemsId = expectedItemIds
                })
            );

        var mediator = mediatorMock.Object;

        var sut = new ItemController(_mapper, mediator);

        // Act

        var result = await sut.List(expectedSearchstring);

        // Assert

        var response = HttpResultAssert.AssertActionResultAsResponse<ItemsResponse>(result);
        Assert.All(response.ItemsId, itemId => response.ItemsId.Contains(itemId));
        mediatorMock.Verify(x => x.Send(It.Is<ListItemsQuery>(y => y.SearchString == expectedSearchstring), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task List_ListItemsWhenSomethingGoesWrong_ShouldFail()
    {
        // Arrange

        var mediatorMock = new Mock<IMediator>();

        var expectedItemIds = new string[] { "item-id-0", "item-id-1" };

        mediatorMock.Setup(x => x.Send(It.IsAny<IRequest<Result<ItemsResult>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ListItemsQuery query, CancellationToken ct) =>
                Result<ItemsResult>.Failure("Something went wrong")
            );

        var mediator = mediatorMock.Object;

        var sut = new ItemController(_mapper, mediator);

        // Act

        var result = await sut.List(string.Empty);

        // Assert

        var response = HttpResultAssert.AssertActionResultAsFailedResponse<FailedResponse>(result);
        Assert.NotEmpty(response.Errors);
    }

    [Fact]
    public async Task GetTradesUsingTheItem_RetrieveTradeIdsThatUseTheGivenItemId_RetrievesTradeIds()
    {
        // Arrange

        var mediatorMock = new Mock<IMediator>();

        var tradeIds = new string[] { "trade-id-0", "trade-id-1" };

        mediatorMock.Setup(x => x.Send(It.IsAny<IRequest<Result<string[]>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetTradesUsingTheItemQuery query, CancellationToken ct) =>
                Result<string[]>.Success(tradeIds)
            );

        var mediator = mediatorMock.Object;

        var sut = new ItemController(_mapper, mediator);

        // Act

        var result = await sut.GetTradesUsingTheItem(_expectedItemId);

        // Assert

        var response = HttpResultAssert.AssertActionResultAsResponse<TradesUsingTheItemResponse>(result);
        Assert.All(response.TradeIds, tradeId => response.TradeIds.Contains(tradeId));
        mediatorMock.Verify(x => x.Send(It.Is<GetTradesUsingTheItemQuery>(y => y.ItemId == _expectedItemId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTradesUsingTheItem_AttemptsRetrievingTradeIdsThatUseTheGivenItemId_ShouldFail()
    {
        // Arrange

        var mediatorMock = new Mock<IMediator>();

        var tradeIds = new string[] { "trade-id-0", "trade-id-1" };

        mediatorMock.Setup(x => x.Send(It.IsAny<IRequest<Result<string[]>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetTradesUsingTheItemQuery query, CancellationToken ct) =>
                Result<string[]>.Failure("Something went wrong")
            );

        var mediator = mediatorMock.Object;

        var sut = new ItemController(_mapper, mediator);

        // Act

        var result = await sut.GetTradesUsingTheItem(_expectedItemId);

        // Assert

        var response = HttpResultAssert.AssertActionResultAsFailedResponse<FailedResponse>(result);
        Assert.NotEmpty(response.Errors);
        mediatorMock.Verify(x => x.Send(It.Is<GetTradesUsingTheItemQuery>(y => y.ItemId == _expectedItemId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_CreateNewItem_CreatesSuccessfully()
    {
        // Arrange

        var mediatorMock = new Mock<IMediator>();

        mediatorMock.Setup(x => x.Send(It.IsAny<IRequest<Result<FullItemResult>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CreateItemCommand query, CancellationToken ct) =>
                Result<FullItemResult>.Success(new FullItemResult
                {
                    ItemId = _expectedItemId,
                    ItemName = _expectedItemName,
                    ItemDescription = _expectedItemDescription
                })
            );

        var mediator = mediatorMock.Object;

        var sut = new ItemController(_mapper, mediator);

        var user = new User
        {
            Id = _expectedSenderUserId,
            UserName = ""
        };

        sut.SetSenderUser(user);

        var request = new CreateItemRequest
        {
            ItemName = _expectedItemName,
            ItemDescription = _expectedItemDescription
        };

        // Act

        var result = await sut.Create(request);

        // Assert

        var response = HttpResultAssert.AssertActionResultAsResponse<CreateItemSuccessResponse>(result);
        Assert.Equal(_expectedItemId, response.ItemId);
        Assert.Equal(_expectedItemName, response.ItemName);
        Assert.Equal(_expectedItemDescription, response.ItemDescription);
        mediatorMock.Verify(x => x.Send(It.Is<CreateItemCommand>(y => y.SenderUserId == _expectedSenderUserId && y.ItemName == _expectedItemName && y.ItemDescription == _expectedItemDescription), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_AttemptsCreatingNewItem_ShouldFail()
    {
        // Arrange

        var mediatorMock = new Mock<IMediator>();

        mediatorMock.Setup(x => x.Send(It.IsAny<IRequest<Result<FullItemResult>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CreateItemCommand query, CancellationToken ct) =>
                Result<FullItemResult>.Failure("Something went wrong")
            );

        var mediator = mediatorMock.Object;

        var sut = new ItemController(_mapper, mediator);

        var user = new User
        {
            Id = _expectedSenderUserId,
            UserName = ""
        };

        sut.SetSenderUser(user);

        var request = new CreateItemRequest
        {
            ItemName = _expectedItemName,
            ItemDescription = _expectedItemDescription
        };

        // Act

        var result = await sut.Create(request);

        // Assert

        var response = HttpResultAssert.AssertActionResultAsFailedResponse<CreateItemFailedResponse>(result);
        Assert.NotEmpty(response.Errors);
        mediatorMock.Verify(x => x.Send(It.Is<CreateItemCommand>(y => y.SenderUserId == _expectedSenderUserId && y.ItemName == _expectedItemName && y.ItemDescription == _expectedItemDescription), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_UpdateItem_UpdatesSuccessfully()
    {
        // Arrange

        var mediatorMock = new Mock<IMediator>();

        mediatorMock.Setup(x => x.Send(It.IsAny<IRequest<Result<FullItemResult>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UpdateItemCommand query, CancellationToken ct) =>
                Result<FullItemResult>.Success(new FullItemResult
                {
                    ItemId = _expectedItemId,
                    ItemName = _expectedItemName,
                    ItemDescription = _expectedItemDescription
                })
            );

        var mediator = mediatorMock.Object;

        var sut = new ItemController(_mapper, mediator);

        var user = new User
        {
            Id = _expectedSenderUserId,
            UserName = ""
        };

        sut.SetSenderUser(user);

        var request = new UpdateItemRequest
        {
            ItemId = _expectedItemId,
            ItemName = _expectedItemName,
            ItemDescription = _expectedItemDescription
        };

        // Act

        var result = await sut.Update(request);

        // Assert

        var response = HttpResultAssert.AssertActionResultAsResponse<UpdateItemSuccessResponse>(result);
        Assert.Equal(_expectedItemId, response.ItemId);
        Assert.Equal(_expectedItemName, response.ItemName);
        Assert.Equal(_expectedItemDescription, response.ItemDescription);
        mediatorMock.Verify(x => x.Send(It.Is<UpdateItemCommand>(y => y.SenderUserId == _expectedSenderUserId && y.ItemName == _expectedItemName && y.ItemDescription == _expectedItemDescription), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_AttemptsUpdatingItem_ShouldFail()
    {
        // Arrange

        var mediatorMock = new Mock<IMediator>();

        mediatorMock.Setup(x => x.Send(It.IsAny<IRequest<Result<FullItemResult>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UpdateItemCommand query, CancellationToken ct) =>
                Result<FullItemResult>.Failure("Something went wrong")
            );

        var mediator = mediatorMock.Object;

        var sut = new ItemController(_mapper, mediator);

        var user = new User
        {
            Id = _expectedSenderUserId,
            UserName = ""
        };

        sut.SetSenderUser(user);

        var request = new UpdateItemRequest
        {
            ItemId = _expectedItemId,
            ItemName = _expectedItemName,
            ItemDescription = _expectedItemDescription
        };

        // Act

        var result = await sut.Update(request);

        // Assert

        var response = HttpResultAssert.AssertActionResultAsFailedResponse<UpdateItemFailedResponse>(result);
        Assert.NotEmpty(response.Errors);
        mediatorMock.Verify(x => x.Send(It.Is<UpdateItemCommand>(y => y.SenderUserId == _expectedSenderUserId && y.ItemName == _expectedItemName && y.ItemDescription == _expectedItemDescription), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_DeleteItem_UpdatesSuccessfully()
    {
        // Arrange

        var mediatorMock = new Mock<IMediator>();

        mediatorMock.Setup(x => x.Send(It.IsAny<IRequest<Result<DeleteItemResult>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeleteItemCommand query, CancellationToken ct) =>
                Result<DeleteItemResult>.Success(new DeleteItemResult
                {
                    ItemId = _expectedItemId,
                    ItemName = _expectedItemName
                })
            );

        var mediator = mediatorMock.Object;

        var sut = new ItemController(_mapper, mediator);

        var user = new User
        {
            Id = _expectedSenderUserId,
            UserName = ""
        };

        sut.SetSenderUser(user);

        var request = new DeleteItemRequest
        {
            ItemId = _expectedItemId
        };

        // Act

        var result = await sut.Delete(request);

        // Assert

        var response = HttpResultAssert.AssertActionResultAsResponse<DeleteItemSuccessResponse>(result);
        Assert.Equal(_expectedItemId, response.ItemId);
        Assert.Equal(_expectedItemName, response.ItemName);
        mediatorMock.Verify(x => x.Send(It.Is<DeleteItemCommand>(y => y.UserId == _expectedSenderUserId && y.ItemId == _expectedItemId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_AttemptsDeletingItem_ShouldFail()
    {
        // Arrange

        var mediatorMock = new Mock<IMediator>();

        mediatorMock.Setup(x => x.Send(It.IsAny<IRequest<Result<DeleteItemResult>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeleteItemCommand query, CancellationToken ct) =>
                Result<DeleteItemResult>.Failure("Something went wrong")
            );

        var mediator = mediatorMock.Object;

        var sut = new ItemController(_mapper, mediator);

        var user = new User
        {
            Id = _expectedSenderUserId,
            UserName = ""
        };

        sut.SetSenderUser(user);

        var request = new DeleteItemRequest
        {
            ItemId = _expectedItemId
        };

        // Act

        var result = await sut.Delete(request);

        // Assert

        var response = HttpResultAssert.AssertActionResultAsFailedResponse<DeleteItemFailedResponse>(result);
        Assert.NotEmpty(response.Errors);
        mediatorMock.Verify(x => x.Send(It.Is<DeleteItemCommand>(y => y.UserId == _expectedSenderUserId && y.ItemId == _expectedItemId), It.IsAny<CancellationToken>()), Times.Once);
    }
}
