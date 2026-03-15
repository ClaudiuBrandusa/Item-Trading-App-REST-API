using System.Security.Claims;
using Application.Models.Trades;
using Domain.Entities.Identity;
using Item_Trading_App_Contracts.Base.Item;
using Item_Trading_App_Contracts.Requests.Trade;
using Item_Trading_App_Contracts.Responses.Item;
using Item_Trading_App_Contracts.Responses.Trade;
using Item_Trading_App_REST_API.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using Web.API.IntegrationTests.Common.Factories;
using Web.API.IntegrationTests.Controllers.Common;
using static Web.API.IntegrationTests.Controllers.Common.Utils;

namespace Web.API.IntegrationTests.Controllers;

public class TradeControllerTests : IClassFixture<TestAppFactory>
{
    private readonly TestAppFactory _factory;
    
    public TradeControllerTests(TestAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Offer_CreateTradeOffer_ShouldCreateSuccessfully()
    {
        // Arrange
        
        using var dbContext = _factory.GetDatabaseContext();
        (var user, var userClaims) = dbContext.GetUserWithClaimsByName("Claudiu");

        using var controllerPack = CreateControllerPackWithUser(_factory, userClaims);
        var controller = controllerPack.ControllerInstance;

        var itemName = "Andesite";
        var itemDescription = string.Empty;
        var itemQuantity = 10;

        var createdItem = await Scenarios.CreateItem(_factory, itemName, itemDescription);
        
        var itemId = createdItem.ItemId;
        
        await Scenarios.AddItemToInventory(_factory, userClaims, itemId, itemQuantity);

        var receiverUser = dbContext.GetUserByName("Root")!;

        var receiverUserId = receiverUser.Id;
        var tradeItems = new ItemWithPrice[]
        {
            CreateTradeItem(createdItem, 50, itemQuantity)
        };

        var createTradeOfferRequest = new TradeOfferRequest
        {
            TargetUserId = receiverUserId,
            Items = tradeItems
        };

        // Act

        var result = await controller.Offer(createTradeOfferRequest);

        // Assert

        var objectResult = AssertActionResultAsOkObjectResult(result);
        var response = AssertOkObjectResultSuccessResponse<TradeOfferSuccessResponse>(objectResult);
        AssertTradeOfferResponse(user, receiverUser, tradeItems, response!);
    }

    [Fact]
    public async Task Offer_TryCreateTradeOfferWithSameSenderAndReceiver_ShouldFail()
    {
        // Arrange
        
        using var dbContext = _factory.GetDatabaseContext();
        (var user, var userClaims) = dbContext.GetUserWithClaimsByName("Claudiu");
        var userId = user.Id;

        using var controllerPack = CreateControllerPackWithUser(_factory, userClaims);
        var controller = controllerPack.ControllerInstance;

        var itemName = "Diorite";
        var itemDescription = string.Empty;
        var itemQuantity = 10;

        var createdItem = await Scenarios.CreateItem(_factory, itemName, itemDescription);
        
        var itemId = createdItem.ItemId;
        
        await Scenarios.AddItemToInventory(_factory, userClaims, itemId, itemQuantity);

        var tradeItems = new ItemWithPrice[]
        {
            CreateTradeItem(createdItem, 50, itemQuantity)
        };

        var createTradeOfferRequest = new TradeOfferRequest
        {
            TargetUserId = userId,
            Items = tradeItems
        };

        // Act

        var result = await controller.Offer(createTradeOfferRequest);

        // Assert

        var objectResult = AssertActionResultAsBadRequestObjectResult(result);
        var response = AssertBadRequestObjectResultFailedResponse<TradeOfferFailedResponse>(objectResult);
        Assert.NotNull(response!.Errors);
        Assert.Single(response!.Errors);
    }

    [Fact]
    public async Task Offer_TryCreateTradeOfferWithoutTradeItems_ShouldFail()
    {
        // Arrange
        
        using var dbContext = _factory.GetDatabaseContext();
        (var user, var userClaims) = dbContext.GetUserWithClaimsByName("Claudiu");

        using var controllerPack = CreateControllerPackWithUser(_factory, userClaims);
        var controller = controllerPack.ControllerInstance;

        var receiverUser = dbContext.GetUserByName("Root")!;

        var receiverUserId = receiverUser.Id;

        var createTradeOfferRequest = new TradeOfferRequest
        {
            TargetUserId = receiverUserId,
            Items = Array.Empty<ItemWithPrice>()
        };

        // Act

        var result = await controller.Offer(createTradeOfferRequest);

        // Assert

        var objectResult = AssertActionResultAsBadRequestObjectResult(result);
        var response = AssertBadRequestObjectResultFailedResponse<TradeOfferFailedResponse>(objectResult);
        Assert.NotNull(response!.Errors);
        Assert.Single(response!.Errors);
    }

    [Fact]
    public async Task Get_CreateTradeOfferThenGetTradeById_ShouldGetSuccessfully()
    {
        // Arrange
        
        using var dbContext = _factory.GetDatabaseContext();
        (var user, var userClaims) = dbContext.GetUserWithClaimsByName("Claudiu");

        using var controllerPack = CreateControllerPackWithUser(_factory, userClaims);
        var controller = controllerPack.ControllerInstance;

        var itemName = "Slate";
        var itemDescription = string.Empty;
        var itemQuantity = 10;

        var createdItem = await Scenarios.CreateItem(_factory, itemName, itemDescription);
        
        var itemId = createdItem.ItemId;
        
        await Scenarios.AddItemToInventory(_factory, userClaims, itemId, itemQuantity);

        var receiverUser = dbContext.GetUserByName("Root")!;

        var receiverUserId = receiverUser.Id;
        var tradeItems = new ItemWithPrice[]
        {
            CreateTradeItem(createdItem, 50, itemQuantity)
        };

        var createdTrade = await Scenarios.CreateTrade(controller, receiverUserId, tradeItems);

        // Act

        var result = await controller.Get(createdTrade!.TradeId);

        // Assert

        var objectResult = AssertActionResultAsOkObjectResult(result);
        var response = AssertOkObjectResultSuccessResponse<TradeOfferSuccessResponse>(objectResult);
        AssertTradeOfferResponse(user, receiverUser, tradeItems, response!);
    }

    [Fact]
    public async Task Get_TryGetTradeThatDoesntExist_ShouldFail()
    {
        // Arrange
        
        using var dbContext = _factory.GetDatabaseContext();
        (var user, var userClaims) = dbContext.GetUserWithClaimsByName("Claudiu");

        using var controllerPack = CreateControllerPackWithUser(_factory, userClaims);
        var controller = controllerPack.ControllerInstance;

        // Act

        var result = await controller.Get(string.Empty);

        // Assert

        var objectResult = AssertActionResultAsBadRequestObjectResult(result);
        var response = AssertBadRequestObjectResultFailedResponse<TradeOfferFailedResponse>(objectResult);
        Assert.NotNull(response!.Errors);
        Assert.Single(response!.Errors);
    }

    [Fact]
    public async Task Accept_CreateTradeOfferThenAccept_ShouldAcceptSuccessfully()
    {
        // Arrange
        
        using var dbContext = _factory.GetDatabaseContext();
        (var user, var userClaims) = dbContext.GetUserWithClaimsByName("Claudiu");

        var itemName = "Quartzite";
        var itemDescription = string.Empty;
        var itemQuantity = 10;

        var createdItem = await Scenarios.CreateItem(_factory, itemName, itemDescription);
        
        var itemId = createdItem.ItemId;
        
        await Scenarios.AddItemToInventory(_factory, userClaims, itemId, itemQuantity);

        (var receiverUser, var receiverUserClaims) = dbContext.GetUserWithClaimsByName("Root");

        using var receiverControllerPack = CreateControllerPackWithUser(_factory, receiverUserClaims);
        var receiverController = receiverControllerPack.ControllerInstance;

        var receiverUserId = receiverUser.Id;
        var tradeItems = new ItemWithPrice[]
        {
            CreateTradeItem(createdItem, 50, itemQuantity)
        };

        var createdTrade = await Scenarios.CreateTrade(_factory, userClaims, receiverUserId, tradeItems);

        var request = new AcceptTradeOfferRequest
        {
            TradeId = createdTrade.TradeId
        };

        // Act

        var result = await receiverController.Accept(request);

        // Assert

        var objectResult = AssertActionResultAsOkObjectResult(result);
        var response = AssertOkObjectResultSuccessResponse<AcceptTradeOfferSuccessResponse>(objectResult)!;
        Assert.Equal(createdTrade.TradeId, response.TradeId);
        Assert.Equal(user.Id, response.SenderId);
        Assert.Equal(user.UserName, response.SenderName);
    }

    [Fact]
    public async Task Accept_TryAcceptTradeThatDoesntExist_ShouldFail()
    {
        // Arrange
        
        using var dbContext = _factory.GetDatabaseContext();
        (var user, var userClaims) = dbContext.GetUserWithClaimsByName("Claudiu");

        using var controllerPack = CreateControllerPackWithUser(_factory, userClaims);
        var controller = controllerPack.ControllerInstance;

        var request = new AcceptTradeOfferRequest
        {
            TradeId = "wrong-trade-id"
        };

        // Act

        var result = await controller.Accept(request);

        // Assert

        var objectResult = AssertActionResultAsBadRequestObjectResult(result);
        var response = AssertBadRequestObjectResultFailedResponse<AcceptTradeOfferFailedResponse>(objectResult);
        Assert.NotNull(response!.Errors);
        Assert.Single(response!.Errors);
    }
    
    [Fact]
    public async Task Accept_TryAcceptTradeThatWasAlreadyAccepted_ShouldFail()
    {
        // Arrange
        
        using var dbContext = _factory.GetDatabaseContext();
        (var user, var userClaims) = dbContext.GetUserWithClaimsByName("Claudiu");

        var itemName = "Limestone";
        var itemDescription = string.Empty;
        var itemQuantity = 10;

        var createdItem = await Scenarios.CreateItem(_factory, itemName, itemDescription);
        
        var itemId = createdItem.ItemId;
        
        await Scenarios.AddItemToInventory(_factory, userClaims, itemId, itemQuantity);

        (var receiverUser, var receiverUserClaims) = dbContext.GetUserWithClaimsByName("Root");

        using var receiverControllerPack = CreateControllerPackWithUser(_factory, receiverUserClaims);
        var receiverController = receiverControllerPack.ControllerInstance;

        var receiverUserId = receiverUser.Id;
        var tradeItems = new ItemWithPrice[]
        {
            CreateTradeItem(createdItem, 50, itemQuantity)
        };

        var createdTrade = await Scenarios.CreateTrade(_factory, userClaims, receiverUserId, tradeItems);

        var request = new AcceptTradeOfferRequest
        {
            TradeId = createdTrade.TradeId
        };

        // Act

        await Scenarios.AcceptTrade(receiverController, createdTrade.TradeId);

        var result = await receiverController.Accept(request);

        // Assert

        var objectResult = AssertActionResultAsBadRequestObjectResult(result);
        var response = AssertBadRequestObjectResultFailedResponse<AcceptTradeOfferFailedResponse>(objectResult);
        Assert.NotNull(response!.Errors);
        Assert.Single(response!.Errors);
    }

    [Fact]
    public async Task Accept_TryAcceptTradeThatWasCanceled_ShouldFail()
    {
        // Arrange
        
        using var dbContext = _factory.GetDatabaseContext();
        (var user, var userClaims) = dbContext.GetUserWithClaimsByName("Claudiu");

        var itemName = "Basalt";
        var itemDescription = string.Empty;
        var itemQuantity = 10;

        var createdItem = await Scenarios.CreateItem(_factory, itemName, itemDescription);
        
        var itemId = createdItem.ItemId;
        
        await Scenarios.AddItemToInventory(_factory, userClaims, itemId, itemQuantity);

        (var receiverUser, var receiverUserClaims) = dbContext.GetUserWithClaimsByName("Root");

        using var receiverControllerPack = CreateControllerPackWithUser(_factory, receiverUserClaims);
        var receiverController = receiverControllerPack.ControllerInstance;

        var receiverUserId = receiverUser.Id;
        var tradeItems = new ItemWithPrice[]
        {
            CreateTradeItem(createdItem, 50, itemQuantity)
        };

        var createdTrade = await Scenarios.CreateTrade(_factory, userClaims, receiverUserId, tradeItems);

        var cancelRequest = new CancelTradeOfferRequest
        {
            TradeId = createdTrade.TradeId
        };

        var acceptRequest = new AcceptTradeOfferRequest
        {
            TradeId = createdTrade.TradeId
        };

        // Act

        await Scenarios.CancelTrade(_factory, userClaims, createdTrade.TradeId);

        var result = await receiverController.Accept(acceptRequest);

        // Assert

        var objectResult = AssertActionResultAsBadRequestObjectResult(result);
        var response = AssertBadRequestObjectResultFailedResponse<AcceptTradeOfferFailedResponse>(objectResult);
        Assert.NotNull(response!.Errors);
        Assert.Single(response!.Errors);
    }

    [Fact]
    public async Task Accept_TryAcceptTheTradeAsSender_ShouldFail()
    {
        // Arrange
        
        using var dbContext = _factory.GetDatabaseContext();
        (var user, var userClaims) = dbContext.GetUserWithClaimsByName("Claudiu");

        using var controllerPack = CreateControllerPackWithUser(_factory, userClaims);
        var controller = controllerPack.ControllerInstance;

        var itemName = "Marble";
        var itemDescription = string.Empty;
        var itemQuantity = 10;

        var createdItem = await Scenarios.CreateItem(_factory, itemName, itemDescription);
        
        var itemId = createdItem.ItemId;
        
        await Scenarios.AddItemToInventory(_factory, userClaims, itemId, itemQuantity);

        var receiverUser = dbContext.GetUserByName("Root")!;

        var receiverUserId = receiverUser.Id;
        var tradeItems = new ItemWithPrice[]
        {
            CreateTradeItem(createdItem, 50, itemQuantity)
        };

        var createdTrade = await Scenarios.CreateTrade(controller, receiverUserId, tradeItems);

        var request = new AcceptTradeOfferRequest
        {
            TradeId = createdTrade.TradeId
        };

        // Act

        var result = await controller.Accept(request);

        // Assert

        var objectResult = AssertActionResultAsBadRequestObjectResult(result);
        var response = AssertBadRequestObjectResultFailedResponse<AcceptTradeOfferFailedResponse>(objectResult);
        Assert.NotNull(response!.Errors);
        Assert.Single(response!.Errors);
    }

    [Fact]
    public async Task Reject_CreateTradeOfferThenReject_ShouldRejectSuccessfully()
    {
        // Arrange
        
        using var dbContext = _factory.GetDatabaseContext();
        (var user, var userClaims) = dbContext.GetUserWithClaimsByName("Claudiu");

        var itemName = "Calcite";
        var itemDescription = string.Empty;
        var itemQuantity = 10;

        var createdItem = await Scenarios.CreateItem(_factory, itemName, itemDescription);
        
        var itemId = createdItem.ItemId;
        
        await Scenarios.AddItemToInventory(_factory, userClaims, itemId, itemQuantity);

        (var receiverUser, var receiverUserClaims) = dbContext.GetUserWithClaimsByName("Root");

        using var receiverControllerPack = CreateControllerPackWithUser(_factory, receiverUserClaims);
        var receiverController = receiverControllerPack.ControllerInstance;

        var receiverUserId = receiverUser.Id;
        var tradeItems = new ItemWithPrice[]
        {
            CreateTradeItem(createdItem, 50, itemQuantity)
        };

        var createdTrade = await Scenarios.CreateTrade(_factory, userClaims, receiverUserId, tradeItems);

        var request = new RejectTradeOfferRequest
        {
            TradeId = createdTrade.TradeId
        };

        // Act

        var result = await receiverController.Reject(request);

        // Assert

        var objectResult = AssertActionResultAsOkObjectResult(result);
        var response = AssertOkObjectResultSuccessResponse<RejectTradeOfferSuccessResponse>(objectResult)!;
        Assert.Equal(createdTrade.TradeId, response.TradeId);
        Assert.Equal(user.Id, response.SenderId);
        Assert.Equal(user.UserName, response.SenderName);
    }

    [Fact]
    public async Task Reject_TryRejectTradeThatDoesntExist_ShouldFail()
    {
        // Arrange
        
        using var dbContext = _factory.GetDatabaseContext();
        (var user, var userClaims) = dbContext.GetUserWithClaimsByName("Claudiu");

        using var controllerPack = CreateControllerPackWithUser(_factory, userClaims);
        var controller = controllerPack.ControllerInstance;

        var request = new RejectTradeOfferRequest
        {
            TradeId = "wrong-trade-id"
        };

        // Act

        var result = await controller.Reject(request);

        // Assert

        var objectResult = AssertActionResultAsBadRequestObjectResult(result);
        var response = AssertBadRequestObjectResultFailedResponse<RejectTradeOfferFailedResponse>(objectResult);
        Assert.NotNull(response!.Errors);
        Assert.Single(response!.Errors);
    }
    
    [Fact]
    public async Task Reject_TryRejectTradeThatWasAlreadyRejected_ShouldFail()
    {
        // Arrange
        
        using var dbContext = _factory.GetDatabaseContext();
        (var user, var userClaims) = dbContext.GetUserWithClaimsByName("Claudiu");

        var itemName = "Feldspar";
        var itemDescription = string.Empty;
        var itemQuantity = 10;

        var createdItem = await Scenarios.CreateItem(_factory, itemName, itemDescription);
        
        var itemId = createdItem.ItemId;
        
        await Scenarios.AddItemToInventory(_factory, userClaims, itemId, itemQuantity);

        (var receiverUser, var receiverUserClaims) = dbContext.GetUserWithClaimsByName("Root");

        using var receiverControllerPack = CreateControllerPackWithUser(_factory, receiverUserClaims);
        var receiverController = receiverControllerPack.ControllerInstance;

        var receiverUserId = receiverUser.Id;
        var tradeItems = new ItemWithPrice[]
        {
            CreateTradeItem(createdItem, 50, itemQuantity)
        };

        var createdTrade = await Scenarios.CreateTrade(_factory, userClaims, receiverUserId, tradeItems);

        var request = new RejectTradeOfferRequest
        {
            TradeId = createdTrade.TradeId
        };

        // Act

        await Scenarios.RejectTrade(receiverController, createdTrade.TradeId);

        var result = await receiverController.Reject(request);

        // Assert

        var objectResult = AssertActionResultAsBadRequestObjectResult(result);
        var response = AssertBadRequestObjectResultFailedResponse<RejectTradeOfferFailedResponse>(objectResult);
        Assert.NotNull(response!.Errors);
        Assert.Single(response!.Errors);
    }

    [Fact]
    public async Task Reject_TryRejectTradeThatWasCanceled_ShouldFail()
    {
        // Arrange
        
        using var dbContext = _factory.GetDatabaseContext();
        (var user, var userClaims) = dbContext.GetUserWithClaimsByName("Claudiu");

        var itemName = "Gravel";
        var itemDescription = string.Empty;
        var itemQuantity = 10;

        var createdItem = await Scenarios.CreateItem(_factory, itemName, itemDescription);
        
        var itemId = createdItem.ItemId;
        
        await Scenarios.AddItemToInventory(_factory, userClaims, itemId, itemQuantity);

        (var receiverUser, var receiverUserClaims) = dbContext.GetUserWithClaimsByName("Root");

        using var receiverControllerPack = CreateControllerPackWithUser(_factory, receiverUserClaims);
        var receiverController = receiverControllerPack.ControllerInstance;

        var receiverUserId = receiverUser.Id;
        var tradeItems = new ItemWithPrice[]
        {
            CreateTradeItem(createdItem, 50, itemQuantity)
        };

        var createdTrade = await Scenarios.CreateTrade(_factory, userClaims, receiverUserId, tradeItems);

        var cancelRequest = new CancelTradeOfferRequest
        {
            TradeId = createdTrade.TradeId
        };

        var rejectRequest = new RejectTradeOfferRequest
        {
            TradeId = createdTrade.TradeId
        };

        // Act

        await Scenarios.CancelTrade(_factory, userClaims, createdTrade.TradeId);

        var result = await receiverController.Reject(rejectRequest);

        // Assert

        var objectResult = AssertActionResultAsBadRequestObjectResult(result);
        var response = AssertBadRequestObjectResultFailedResponse<RejectTradeOfferFailedResponse>(objectResult);
        Assert.NotNull(response!.Errors);
        Assert.Single(response!.Errors);
    }

    [Fact]
    public async Task Reject_TryRejectTheTradeAsSender_ShouldFail()
    {
        // Arrange
        
        using var dbContext = _factory.GetDatabaseContext();
        (var user, var userClaims) = dbContext.GetUserWithClaimsByName("Claudiu");

        using var controllerPack = CreateControllerPackWithUser(_factory, userClaims);
        var controller = controllerPack.ControllerInstance;

        var itemName = "Dolomite";
        var itemDescription = string.Empty;
        var itemQuantity = 10;

        var createdItem = await Scenarios.CreateItem(_factory, itemName, itemDescription);
        
        var itemId = createdItem.ItemId;
        
        await Scenarios.AddItemToInventory(_factory, userClaims, itemId, itemQuantity);

        var receiverUser = dbContext.GetUserByName("Root")!;

        var receiverUserId = receiverUser.Id;
        var tradeItems = new ItemWithPrice[]
        {
            CreateTradeItem(createdItem, 50, itemQuantity)
        };

        var createdTrade = await Scenarios.CreateTrade(controller, receiverUserId, tradeItems);

        var request = new RejectTradeOfferRequest
        {
            TradeId = createdTrade.TradeId
        };

        // Act

        var result = await controller.Reject(request);

        // Assert

        var objectResult = AssertActionResultAsBadRequestObjectResult(result);
        var response = AssertBadRequestObjectResultFailedResponse<RejectTradeOfferFailedResponse>(objectResult);
        Assert.NotNull(response!.Errors);
        Assert.Single(response!.Errors);
    }

    [Fact]
    public async Task Cancel_CreateTradeOfferThenCancel_ShouldCancelSuccessfully()
    {
        // Arrange
        
        using var dbContext = _factory.GetDatabaseContext();
        (var user, var userClaims) = dbContext.GetUserWithClaimsByName("Claudiu");

        using var controllerPack = CreateControllerPackWithUser(_factory, userClaims);
        var controller = controllerPack.ControllerInstance;

        var itemName = "Aragonite";
        var itemDescription = string.Empty;
        var itemQuantity = 10;

        var createdItem = await Scenarios.CreateItem(_factory, itemName, itemDescription);
        
        var itemId = createdItem.ItemId;
        
        await Scenarios.AddItemToInventory(_factory, userClaims, itemId, itemQuantity);

        var receiverUser = dbContext.GetUserByName("Root")!;

        var receiverUserId = receiverUser.Id;
        var tradeItems = new ItemWithPrice[]
        {
            CreateTradeItem(createdItem, 50, itemQuantity)
        };

        var createdTrade = await Scenarios.CreateTrade(controller, receiverUserId, tradeItems);

        var request = new CancelTradeOfferRequest
        {
            TradeId = createdTrade.TradeId
        };

        // Act

        var result = await controller.Cancel(request);

        // Assert

        var objectResult = AssertActionResultAsOkObjectResult(result);
        var response = AssertOkObjectResultSuccessResponse<CancelTradeOfferSuccessResponse>(objectResult)!;
        Assert.Equal(createdTrade.TradeId, response.TradeId);
        Assert.Equal(receiverUserId, response.ReceiverId);
        Assert.Equal(receiverUser.UserName, response.ReceiverName);
    }

    [Fact]
    public async Task Cancel_TryCancelTradeThatDoesntExist_ShouldFail()
    {
        // Arrange
        
        using var dbContext = _factory.GetDatabaseContext();
        (var user, var userClaims) = dbContext.GetUserWithClaimsByName("Claudiu");

        using var controllerPack = CreateControllerPackWithUser(_factory, userClaims);
        var controller = controllerPack.ControllerInstance;

        var request = new CancelTradeOfferRequest
        {
            TradeId = "wrong-trade-id"
        };

        // Act

        var result = await controller.Cancel(request);

        // Assert

        var objectResult = AssertActionResultAsBadRequestObjectResult(result);
        var response = AssertBadRequestObjectResultFailedResponse<CancelTradeOfferFailedResponse>(objectResult);
        Assert.NotNull(response!.Errors);
        Assert.Single(response!.Errors);
    }
    
    [Fact]
    public async Task Cancel_TryCancelTradeThatWasAlreadyCanceled_ShouldFail()
    {
        // Arrange
        
        using var dbContext = _factory.GetDatabaseContext();
        (var user, var userClaims) = dbContext.GetUserWithClaimsByName("Claudiu");

        using var controllerPack = CreateControllerPackWithUser(_factory, userClaims);
        var controller = controllerPack.ControllerInstance;

        var itemName = "Anhydrite";
        var itemDescription = string.Empty;
        var itemQuantity = 10;

        var createdItem = await Scenarios.CreateItem(_factory, itemName, itemDescription);
        
        var itemId = createdItem.ItemId;
        
        await Scenarios.AddItemToInventory(_factory, userClaims, itemId, itemQuantity);

        (var receiverUser, var receiverUserClaims) = dbContext.GetUserWithClaimsByName("Root");

        var receiverUserId = receiverUser.Id;
        var tradeItems = new ItemWithPrice[]
        {
            CreateTradeItem(createdItem, 50, itemQuantity)
        };

        var createdTrade = await Scenarios.CreateTrade(controller, receiverUserId, tradeItems);

        var request = new CancelTradeOfferRequest
        {
            TradeId = createdTrade.TradeId
        };

        // Act

        await controller.Cancel(request);

        var result = await controller.Cancel(request);

        // Assert

        var objectResult = AssertActionResultAsBadRequestObjectResult(result);
        var response = AssertBadRequestObjectResultFailedResponse<CancelTradeOfferFailedResponse>(objectResult);
        Assert.NotNull(response!.Errors);
        Assert.Single(response!.Errors);
    }

    [Fact]
    public async Task Cancel_TryCancelTradeThatWasAnswered_ShouldFail()
    {
        // Arrange
        
        using var dbContext = _factory.GetDatabaseContext();
        (var user, var userClaims) = dbContext.GetUserWithClaimsByName("Claudiu");

        using var controllerPack = CreateControllerPackWithUser(_factory, userClaims);
        var controller = controllerPack.ControllerInstance;

        var itemName = "Gypsum";
        var itemDescription = string.Empty;
        var itemQuantity = 10;

        var createdItem = await Scenarios.CreateItem(_factory, itemName, itemDescription);
        
        var itemId = createdItem.ItemId;
        
        await Scenarios.AddItemToInventory(_factory, userClaims, itemId, itemQuantity);

        (var receiverUser, var receiverUserClaims) = dbContext.GetUserWithClaimsByName("Root");

        var receiverUserId = receiverUser.Id;
        var tradeItems = new ItemWithPrice[]
        {
            CreateTradeItem(createdItem, 50, itemQuantity)
        };

        var createdTrade = await Scenarios.CreateTrade(controller, receiverUserId, tradeItems);

        var acceptRequest = new AcceptTradeOfferRequest
        {
            TradeId = createdTrade.TradeId
        };

        var cancelRequest = new CancelTradeOfferRequest
        {
            TradeId = createdTrade.TradeId
        };

        // Act

        await Scenarios.AcceptTrade(_factory, receiverUserClaims, createdTrade.TradeId);

        var result = await controller.Cancel(cancelRequest);

        // Assert

        var objectResult = AssertActionResultAsBadRequestObjectResult(result);
        var response = AssertBadRequestObjectResultFailedResponse<CancelTradeOfferFailedResponse>(objectResult);
        Assert.NotNull(response!.Errors);
        Assert.Single(response!.Errors);
    }

    [Fact]
    public async Task Cancel_TryCancelTheTradeAsReceiver_ShouldFail()
    {
        // Arrange
        
        using var dbContext = _factory.GetDatabaseContext();
        (var user, var userClaims) = dbContext.GetUserWithClaimsByName("Claudiu");

        var itemName = "Gneiss";
        var itemDescription = string.Empty;
        var itemQuantity = 10;

        var createdItem = await Scenarios.CreateItem(_factory, itemName, itemDescription);
        
        var itemId = createdItem.ItemId;
        
        await Scenarios.AddItemToInventory(_factory, userClaims, itemId, itemQuantity);

        (var receiverUser, var receiverUserClaims) = dbContext.GetUserWithClaimsByName("Root");

        using var receiverControllerPack = CreateControllerPackWithUser(_factory, receiverUserClaims);
        var receiverController = receiverControllerPack.ControllerInstance;

        var receiverUserId = receiverUser.Id;
        var tradeItems = new ItemWithPrice[]
        {
            CreateTradeItem(createdItem, 50, itemQuantity)
        };

        var createdTrade = await Scenarios.CreateTrade(_factory, userClaims, receiverUserId, tradeItems);

        var request = new CancelTradeOfferRequest
        {
            TradeId = createdTrade.TradeId
        };

        // Act

        var result = await receiverController.Cancel(request);

        // Assert

        var objectResult = AssertActionResultAsBadRequestObjectResult(result);
        var response = AssertBadRequestObjectResultFailedResponse<CancelTradeOfferFailedResponse>(objectResult);
        Assert.NotNull(response!.Errors);
        Assert.Single(response!.Errors);
    }

    [Fact]
    public async Task GetTradeDirections_GetTheTradeDirectionsAvailable_ReturnsSuccessfully()
    {
        // Arrange

        var expectedTradeDirections = Enum.GetNames(typeof(TradeDirection));
        
        using var dbContext = _factory.GetDatabaseContext();
        (var user, var userClaims) = dbContext.GetUserWithClaimsByName("Claudiu");

        using var controllerPack = CreateControllerPackWithUser(_factory, userClaims);
        var controller = controllerPack.ControllerInstance;

        // Act

        var result = await controller.GetTradeDirections();

        // Assert

        var objectResult = AssertActionResultAsOkObjectResult(result);
        var tradeDirections = AssertOkObjectResultSuccessResponse<string[]>(objectResult);
        Assert.NotNull(tradeDirections);
        Assert.All(expectedTradeDirections, expectedTradeDirection => tradeDirections.Contains(expectedTradeDirection));
    }

    [Fact]
    public async Task List_CreateSomeTradesThenListThem_ShouldListSuccessfully()
    {
        // Arrange
        
        using var dbContext = _factory.GetDatabaseContext();
        (var user, var userClaims) = dbContext.GetUserWithClaimsByName("Claudiu");
        (var receiverUser, var receiverUserClaims) = dbContext.GetUserWithClaimsByName("Root");
        var receiverUserId = receiverUser.Id;

        using var controllerPack = CreateControllerPackWithUser(_factory, userClaims);
        var controller = controllerPack.ControllerInstance;

        var itemName = "Tonalite";
        var itemDescription = string.Empty;
        var itemQuantity = 10;

        var createdItem = await Scenarios.CreateItem(_factory, itemName, itemDescription);
        
        var itemId = createdItem.ItemId;
        
        await Scenarios.AddItemToInventory(_factory, userClaims, itemId, itemQuantity);
        await Scenarios.AddItemToInventory(_factory, receiverUserClaims, itemId, itemQuantity);

        var tradeItems = new ItemWithPrice[]
        {
            CreateTradeItem(createdItem, 50, itemQuantity)
        };

        var createdTradeFromSender = await Scenarios.CreateTrade(controller, receiverUserId, tradeItems);
        var createdTradeFromReceiver = await Scenarios.CreateTrade(_factory, receiverUserClaims, user.Id, tradeItems);

        // Act

        var result = await controller.List([createdItem.ItemId], TradeDirection.All.ToString());

        // Assert

        var objectResult = AssertActionResultAsOkObjectResult(result);
        var response = AssertOkObjectResultSuccessResponse<ListTradeOffersSuccessResponse>(objectResult);
        Assert.NotNull(response);
        var sentTradeIds = response.SentTradeOfferIds.ToArray();
        var receivedTradeIds = response.ReceivedTradeOfferIds.ToArray();
        Assert.True(sentTradeIds.Length > 0);
        Assert.True(receivedTradeIds.Length > 0);
        Assert.Contains(createdTradeFromSender.TradeId, sentTradeIds);
        Assert.DoesNotContain(createdTradeFromSender.TradeId, receivedTradeIds);
        Assert.Contains(createdTradeFromReceiver.TradeId, receivedTradeIds);
        Assert.DoesNotContain(createdTradeFromReceiver.TradeId, sentTradeIds);
    }
    
    [Fact]
    public async Task List_CreateSomeTradesThenListOnlySentTrades_ShouldListSuccessfully()
    {
        // Arrange
        
        using var dbContext = _factory.GetDatabaseContext();
        (var user, var userClaims) = dbContext.GetUserWithClaimsByName("Claudiu");
        (var receiverUser, var receiverUserClaims) = dbContext.GetUserWithClaimsByName("Root");
        var receiverUserId = receiverUser.Id;

        using var controllerPack = CreateControllerPackWithUser(_factory, userClaims);
        var controller = controllerPack.ControllerInstance;

        var itemName = "Dacite";
        var itemDescription = string.Empty;
        var itemQuantity = 10;

        var createdItem = await Scenarios.CreateItem(_factory, itemName, itemDescription);
        
        var itemId = createdItem.ItemId;
        
        await Scenarios.AddItemToInventory(_factory, userClaims, itemId, itemQuantity);
        await Scenarios.AddItemToInventory(_factory, receiverUserClaims, itemId, itemQuantity);

        var tradeItems = new ItemWithPrice[]
        {
            CreateTradeItem(createdItem, 50, itemQuantity)
        };

        var createdTradeFromSender = await Scenarios.CreateTrade(controller, receiverUserId, tradeItems);
        var createdTradeFromReceiver = await Scenarios.CreateTrade(_factory, receiverUserClaims, user.Id, tradeItems);

        // Act

        var result = await controller.List([createdItem.ItemId], TradeDirection.Sent.ToString());

        // Assert

        var objectResult = AssertActionResultAsOkObjectResult(result);
        var response = AssertOkObjectResultSuccessResponse<ListTradeOffersSuccessResponse>(objectResult);
        Assert.NotNull(response);
        var sentTradeIds = response.SentTradeOfferIds.ToArray();
        var receivedTradeIds = response.ReceivedTradeOfferIds.ToArray();
        Assert.True(sentTradeIds.Length > 0);
        Assert.Empty(receivedTradeIds);
        Assert.Contains(createdTradeFromSender.TradeId, sentTradeIds);
    }

    [Fact]
    public async Task List_CreateSomeTradesThenListOnlyReceivedTrades_ShouldListSuccessfully()
    {
        // Arrange
        
        using var dbContext = _factory.GetDatabaseContext();
        (var user, var userClaims) = dbContext.GetUserWithClaimsByName("Claudiu");
        (var receiverUser, var receiverUserClaims) = dbContext.GetUserWithClaimsByName("Root");
        var receiverUserId = receiverUser.Id;

        using var controllerPack = CreateControllerPackWithUser(_factory, userClaims);
        var controller = controllerPack.ControllerInstance;

        var itemName = "Basanite";
        var itemDescription = string.Empty;
        var itemQuantity = 10;

        var createdItem = await Scenarios.CreateItem(_factory, itemName, itemDescription);
        
        var itemId = createdItem.ItemId;
        
        await Scenarios.AddItemToInventory(_factory, userClaims, itemId, itemQuantity);
        await Scenarios.AddItemToInventory(_factory, receiverUserClaims, itemId, itemQuantity);

        var tradeItems = new ItemWithPrice[]
        {
            CreateTradeItem(createdItem, 50, itemQuantity)
        };

        var createdTradeFromSender = await Scenarios.CreateTrade(controller, receiverUserId, tradeItems);
        var createdTradeFromReceiver = await Scenarios.CreateTrade(_factory, receiverUserClaims, user.Id, tradeItems);

        // Act

        var result = await controller.List([createdItem.ItemId], TradeDirection.Received.ToString());

        // Assert

        var objectResult = AssertActionResultAsOkObjectResult(result);
        var response = AssertOkObjectResultSuccessResponse<ListTradeOffersSuccessResponse>(objectResult);
        Assert.NotNull(response);
        var sentTradeIds = response.SentTradeOfferIds.ToArray();
        var receivedTradeIds = response.ReceivedTradeOfferIds.ToArray();
        Assert.Empty(sentTradeIds);
        Assert.True(receivedTradeIds.Length > 0);
        Assert.Contains(createdTradeFromReceiver.TradeId, receivedTradeIds);
    }

    [Fact]
    public async Task List_CreateSomeTradesAndRespondThemThenListThem_ShouldListSuccessfully()
    {
        // Arrange
        
        using var dbContext = _factory.GetDatabaseContext();
        (var user, var userClaims) = dbContext.GetUserWithClaimsByName("Claudiu");
        (var receiverUser, var receiverUserClaims) = dbContext.GetUserWithClaimsByName("Root");
        var receiverUserId = receiverUser.Id;

        using var controllerPack = CreateControllerPackWithUser(_factory, userClaims);
        var controller = controllerPack.ControllerInstance;

        var itemName = "Granodiorite";
        var itemDescription = string.Empty;
        var itemQuantity = 10;

        var createdItem = await Scenarios.CreateItem(_factory, itemName, itemDescription);
        
        var itemId = createdItem.ItemId;
        
        await Scenarios.AddItemToInventory(_factory, userClaims, itemId, itemQuantity);
        await Scenarios.AddItemToInventory(_factory, receiverUserClaims, itemId, itemQuantity);

        var tradeItems = new ItemWithPrice[]
        {
            CreateTradeItem(createdItem, 50, itemQuantity)
        };

        var createdTradeFromSender = await Scenarios.CreateTrade(controller, receiverUserId, tradeItems);
        var createdTradeFromReceiver = await Scenarios.CreateTrade(_factory, receiverUserClaims, user.Id, tradeItems);

        var x = await Scenarios.AcceptTrade(_factory, receiverUserClaims, createdTradeFromSender.TradeId);
        var y = await Scenarios.AcceptTrade(controller, createdTradeFromReceiver.TradeId);

        // Act

        var result = await controller.List([createdItem.ItemId], TradeDirection.All.ToString(), true);

        // Assert

        var objectResult = AssertActionResultAsOkObjectResult(result);
        var response = AssertOkObjectResultSuccessResponse<ListTradeOffersSuccessResponse>(objectResult);
        Assert.NotNull(response);
        var sentTradeIds = response.SentTradeOfferIds.ToArray();
        var receivedTradeIds = response.ReceivedTradeOfferIds.ToArray();
        Assert.True(sentTradeIds.Length > 0);
        Assert.True(receivedTradeIds.Length > 0);
        Assert.Contains(createdTradeFromSender.TradeId, sentTradeIds);
        Assert.DoesNotContain(createdTradeFromSender.TradeId, receivedTradeIds);
        Assert.Contains(createdTradeFromReceiver.TradeId, receivedTradeIds);
        Assert.DoesNotContain(createdTradeFromReceiver.TradeId, sentTradeIds);
    }

    private ItemWithPrice CreateTradeItem(CreateItemSuccessResponse item, int price, int quantity)
    {
        return new ItemWithPrice
        {
            Id = item.ItemId,
            Name = item.ItemName,
            Price = price,
            Quantity = quantity
        };
    }

    private void AssertTradeOfferResponse(User senderUser, User receiverUser, ItemWithPrice[] tradeItems, TradeOfferSuccessResponse response)
    {
        Assert.Equal(senderUser.Id, response.SenderId);
        Assert.Equal(senderUser.UserName, response.SenderName);
        Assert.Equal(receiverUser.Id, response.ReceiverId);
        Assert.Equal(receiverUser.UserName, response.ReceiverName);
        Assert.Null(response.Response);
        Assert.Null(response.ResponseDate);
        var responseItems = response.Items?.ToArray();
        Assert.NotNull(responseItems);
        Assert.Equal(tradeItems.Length, responseItems.Length);
        Assert.All(responseItems, responseItem =>
        {
            var itemId = responseItem.Id;
            Assert.NotNull(itemId);
            var sentTradeItem = tradeItems.FirstOrDefault(x => x.Id == itemId);
            Assert.NotNull(sentTradeItem);
            Assert.Equal(sentTradeItem.Name, responseItem.Name);
            Assert.Equal(sentTradeItem.Price, responseItem.Price);
            Assert.Equal(sentTradeItem.Quantity, responseItem.Quantity);
        });
    }

    private ControllerPack<TradeController> CreateControllerPackWithUser(TestAppFactory factory, ClaimsPrincipal user)
    {
        return CreateControllerPackWithUser<TradeController>(factory, user);
    }
}
