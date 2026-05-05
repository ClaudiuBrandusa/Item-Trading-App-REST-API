using Application.Behaviors.Inventories.AddItem;
using Application.Behaviors.Inventories.DropItem;
using Application.Behaviors.Inventories.GetItem;
using Application.Behaviors.Inventories.GetLockedAmount;
using Application.Behaviors.Inventories.ListItems;
using Application.Models.Common;
using Application.Results.Inventories;
using Application.Results.Items;
using CommonTestUtils.Assertions;
using CommonTestUtils.Extensions;
using Domain.Entities.Identity;
using Item_Trading_App_Contracts.Requests.Inventory;
using Item_Trading_App_Contracts.Responses.Base;
using Item_Trading_App_Contracts.Responses.Inventory;
using Item_Trading_App_REST_API.Controllers;
using Item_Trading_App_REST_API.MappingConfigs;
using MapsterMapper;
using MediatR;
using Moq;

namespace Web.API.UnitTests.Endpoints;

public class InventoryMappingFixture
{
    public Mapper Mapper { get; }

    public InventoryMappingFixture()
    {
        Mapper = new Mapper();

        var itemMappingConfig = new InventoryMappingConfig();
        itemMappingConfig.Register(Mapper.Config);
        var generalMappingConfig = new GeneralMappingConfig();
        generalMappingConfig.Register(Mapper.Config);
    }
}

public class InventoryControllerTests : IClassFixture<InventoryMappingFixture>
{
    private readonly Mapper _mapper;
    
    public InventoryControllerTests(InventoryMappingFixture fixture)
    {
        _mapper = fixture.Mapper;
    }

    [Fact]
    public async Task Add_AddNewItemToInventory_AddedSuccessfully()
    {
        // Arrange

        var expectedItemResult = new QuantifiedItemResult
        {
            ItemId = "item-id",
            ItemName = "item-name",
            ItemDescription = "item-description",
            Quantity = 10
        };
        
        var mediatorMock = new Mock<IMediator>();

        mediatorMock.Setup(x => x.Send(It.IsAny<AddInventoryItemCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AddInventoryItemCommand command, CancellationToken ct) =>
            Result<QuantifiedItemResult>.Success(expectedItemResult));

        var mediator = mediatorMock.Object;

        var user = new User
        {
            Id = User.GenerateId(),
            UserName = string.Empty
        };

        var sut = new InventoryController(_mapper, mediator);

        sut.SetSenderUser(user);

        var request = new AddItemRequest
        {
            ItemId = expectedItemResult.ItemId,
            Quantity = expectedItemResult.Quantity
        };

        // Act

        var result = await sut.Add(request);

        // Assert

        var response = HttpResultAssert.AssertActionResultAsResponse<AddItemSuccessResponse>(result);
        Assert.Equal(expectedItemResult.ItemId, response.ItemId);
        Assert.Equal(expectedItemResult.ItemName, response.ItemName);
        Assert.Equal(expectedItemResult.Quantity, response.Quantity);
    }

    [Fact]
    public async Task Add_AttemptToAddNewInvalidItemToInventory_ShouldFail()
    {
        // Arrange
        
        var mediatorMock = new Mock<IMediator>();

        mediatorMock.Setup(x => x.Send(It.IsAny<AddInventoryItemCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AddInventoryItemCommand command, CancellationToken ct) =>
            Result<QuantifiedItemResult>.Failure("Something went wrong"));

        var mediator = mediatorMock.Object;

        var user = new User
        {
            Id = User.GenerateId(),
            UserName = string.Empty
        };

        var sut = new InventoryController(_mapper, mediator);

        sut.SetSenderUser(user);

        var request = new AddItemRequest
        {
            ItemId = "item-id",
            Quantity = 10
        };

        // Act

        var result = await sut.Add(request);

        // Assert

        var response = HttpResultAssert.AssertActionResultAsFailedResponse<AddItemFailedResponse>(result);
        Assert.NotEmpty(response.Errors);
    }

    [Fact]
    public async Task Drop_DropItemFromInventory_DroppedSuccessfully()
    {
        // Arrange

        var expectedItemResult = new QuantifiedItemResult
        {
            ItemId = "item-id",
            ItemName = "item-name",
            ItemDescription = "item-description",
            Quantity = 10
        };
        
        var mediatorMock = new Mock<IMediator>();

        mediatorMock.Setup(x => x.Send(It.IsAny<DropInventoryItemCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DropInventoryItemCommand command, CancellationToken ct) =>
            Result<QuantifiedItemResult>.Success(expectedItemResult));

        var mediator = mediatorMock.Object;

        var user = new User
        {
            Id = User.GenerateId(),
            UserName = string.Empty
        };

        var sut = new InventoryController(_mapper, mediator);

        sut.SetSenderUser(user);

        var request = new DropItemRequest
        {
            ItemId = expectedItemResult.ItemId,
            ItemQuantity = expectedItemResult.Quantity
        };

        // Act

        var result = await sut.Drop(request);

        // Assert

        var response = HttpResultAssert.AssertActionResultAsResponse<DropItemSuccessResponse>(result);
        Assert.Equal(expectedItemResult.ItemId, response.ItemId);
        Assert.Equal(expectedItemResult.ItemName, response.ItemName);
        Assert.Equal(expectedItemResult.Quantity, response.Quantity);
    }

    [Fact]
    public async Task Drop_AttemptToDropInvalidItemFromInventory_ShouldFail()
    {
        // Arrange
        
        var mediatorMock = new Mock<IMediator>();

        mediatorMock.Setup(x => x.Send(It.IsAny<DropInventoryItemCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DropInventoryItemCommand command, CancellationToken ct) =>
            Result<QuantifiedItemResult>.Failure("Something went wrong"));

        var mediator = mediatorMock.Object;

        var user = new User
        {
            Id = User.GenerateId(),
            UserName = string.Empty
        };

        var sut = new InventoryController(_mapper, mediator);

        sut.SetSenderUser(user);

        var request = new DropItemRequest
        {
            ItemId = "item-id",
            ItemQuantity = 10
        };

        // Act

        var result = await sut.Drop(request);

        // Assert

        var response = HttpResultAssert.AssertActionResultAsFailedResponse<DropItemFailedResponse>(result);
        Assert.NotEmpty(response.Errors);
    }

    [Fact]
    public async Task Get_GetItemFromInventory_RetrievesItemSuccessfully()
    {
        // Arrange

        var expectedItemResult = new QuantifiedItemResult
        {
            ItemId = "item-id",
            ItemName = "item-name",
            ItemDescription = "item-description",
            Quantity = 10
        };
        
        var mediatorMock = new Mock<IMediator>();

        mediatorMock.Setup(x => x.Send(It.IsAny<GetInventoryItemQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetInventoryItemQuery command, CancellationToken ct) =>
            Result<QuantifiedItemResult>.Success(expectedItemResult));

        var mediator = mediatorMock.Object;

        var user = new User
        {
            Id = User.GenerateId(),
            UserName = string.Empty
        };

        var sut = new InventoryController(_mapper, mediator);

        sut.SetSenderUser(user);

        // Act

        var result = await sut.Get(expectedItemResult.ItemId);

        // Assert

        var response = HttpResultAssert.AssertActionResultAsResponse<GetItemSuccessResponse>(result);
        Assert.Equal(expectedItemResult.ItemId, response.ItemId);
        Assert.Equal(expectedItemResult.ItemName, response.ItemName);
        Assert.Equal(expectedItemResult.Quantity, response.Quantity);
    }

    [Fact]
    public async Task Get_AttemptToRetrieveItemFromInventory_ShouldFail()
    {
        // Arrange
        
        var mediatorMock = new Mock<IMediator>();

        mediatorMock.Setup(x => x.Send(It.IsAny<GetInventoryItemQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetInventoryItemQuery command, CancellationToken ct) =>
            Result<QuantifiedItemResult>.Failure("Something went wrong"));

        var mediator = mediatorMock.Object;

        var user = new User
        {
            Id = User.GenerateId(),
            UserName = string.Empty
        };

        var sut = new InventoryController(_mapper, mediator);

        sut.SetSenderUser(user);

        // Act

        var result = await sut.Get("item-id");

        // Assert

        var response = HttpResultAssert.AssertActionResultAsFailedResponse<GetItemFailedResponse>(result);
        Assert.NotEmpty(response.Errors);
    }

    [Fact]
    public async Task List_ListItemsFromInventory_ListsSuccessfully()
    {
        // Arrange

        var expectedItemsResult = new ItemsResult
        {
            ItemsId = [ "item-id-0", "item-id-1" ]
        };
        
        var mediatorMock = new Mock<IMediator>();

        mediatorMock.Setup(x => x.Send(It.IsAny<ListInventoryItemsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ListInventoryItemsQuery command, CancellationToken ct) =>
            Result<ItemsResult>.Success(expectedItemsResult));

        var mediator = mediatorMock.Object;

        var user = new User
        {
            Id = User.GenerateId(),
            UserName = string.Empty
        };

        var sut = new InventoryController(_mapper, mediator);

        sut.SetSenderUser(user);

        // Act

        var result = await sut.List(string.Empty);

        // Assert

        var response = HttpResultAssert.AssertActionResultAsResponse<ListItemsSuccessResponse>(result);
        Assert.All(expectedItemsResult.ItemsId, itemId => response.ItemsId.Contains(itemId));
    }

    [Fact]
    public async Task List_AttemptToListItemsFromInventory_ShouldFail()
    {
        // Arrange
        
        var mediatorMock = new Mock<IMediator>();

        mediatorMock.Setup(x => x.Send(It.IsAny<ListInventoryItemsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ListInventoryItemsQuery command, CancellationToken ct) =>
            Result<ItemsResult>.Failure("Something went wrong"));

        var mediator = mediatorMock.Object;

        var user = new User
        {
            Id = User.GenerateId(),
            UserName = string.Empty
        };

        var sut = new InventoryController(_mapper, mediator);

        sut.SetSenderUser(user);

        // Act

        var result = await sut.List(string.Empty);

        // Assert

        var response = HttpResultAssert.AssertActionResultAsFailedResponse<FailedResponse>(result);
        Assert.NotEmpty(response.Errors);
    }

    [Fact]
    public async Task GetLockedAmount_GetLockedAmountOfItemFromInventory_RetrievesSuccessfully()
    {
        // Arrange

        var expectedLockedItemAmountResult = new LockedItemAmountResult
        {
            ItemId = "item-id",
            ItemName = "item-name",
            Amount = 10
        };
        
        var mediatorMock = new Mock<IMediator>();

        mediatorMock.Setup(x => x.Send(It.IsAny<GetInventoryItemLockedAmountQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetInventoryItemLockedAmountQuery command, CancellationToken ct) =>
            Result<LockedItemAmountResult>.Success(expectedLockedItemAmountResult));

        var mediator = mediatorMock.Object;

        var user = new User
        {
            Id = User.GenerateId(),
            UserName = string.Empty
        };

        var sut = new InventoryController(_mapper, mediator);

        sut.SetSenderUser(user);

        // Act

        var result = await sut.GetLockedAmount(string.Empty);

        // Assert

        var response = HttpResultAssert.AssertActionResultAsResponse<GetLockedAmountSuccessResponse>(result);
        Assert.Equal(expectedLockedItemAmountResult.ItemId, response.ItemId);
        Assert.Equal(expectedLockedItemAmountResult.ItemName, response.ItemName);
        Assert.Equal(expectedLockedItemAmountResult.Amount, response.LockedAmount);
    }

    [Fact]
    public async Task GetLockedAmount_AttemptToGetLockedAmountOfItemFromInventory_ShouldFail()
    {
        // Arrange
        
        var mediatorMock = new Mock<IMediator>();

        mediatorMock.Setup(x => x.Send(It.IsAny<GetInventoryItemLockedAmountQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetInventoryItemLockedAmountQuery command, CancellationToken ct) =>
            Result<LockedItemAmountResult>.Failure("Something went wrong"));

        var mediator = mediatorMock.Object;

        var user = new User
        {
            Id = User.GenerateId(),
            UserName = string.Empty
        };

        var sut = new InventoryController(_mapper, mediator);

        sut.SetSenderUser(user);

        // Act

        var result = await sut.GetLockedAmount(string.Empty);

        // Assert

        var response = HttpResultAssert.AssertActionResultAsFailedResponse<GetLockedAmountFailedResponse>(result);
        Assert.NotEmpty(response.Errors);
    }
}
