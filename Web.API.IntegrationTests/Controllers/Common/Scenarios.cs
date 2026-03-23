using System.Security.Claims;
using Application.Behaviors.Inventories.LockItem;
using Item_Trading_App_Contracts.Base.Item;
using Item_Trading_App_Contracts.Requests.Inventory;
using Item_Trading_App_Contracts.Requests.Trade;
using Item_Trading_App_Contracts.Responses.Inventory;
using Item_Trading_App_Contracts.Responses.Item;
using Item_Trading_App_Contracts.Responses.Trade;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Web.API.IntegrationTests.Common.Factories;
using static Web.API.IntegrationTests.Controllers.Common.Utils;
using Item_Trading_App_REST_API.Controllers;

namespace Web.API.IntegrationTests.Controllers.Common;

public static class Scenarios
{
    public static async Task CreateUser()
    {
        
    }

    public static async Task<CreateItemSuccessResponse> CreateItem(ItemController controller, string itemName, string itemDescription)
    {
        var request = new Item_Trading_App_Contracts.Requests.Item.CreateItemRequest
        {
            ItemName = itemName,
            ItemDescription = itemDescription
        };

        var createdItemResult = await controller.Create(request);

        var createdItemObjectResult = createdItemResult as OkObjectResult;
        return (createdItemObjectResult!.Value! as CreateItemSuccessResponse)!;
    }

    public static async Task<CreateItemSuccessResponse> CreateItem(TestAppFactory factory, string itemName, string itemDescription)
    {
        using var controllerPack = CreateControllerPackWithDefaultUser<ItemController>(factory);

        return await CreateItem(controllerPack.ControllerInstance, itemName, itemDescription);
    }

    public static async Task<AddItemSuccessResponse> AddItemToInventory(InventoryController controller, string itemId, int quantity)
    {
        var request = new AddItemRequest { ItemId = itemId, Quantity = quantity };

        var addedItemResult = await controller.Add(request);

        var addedItemObjectResult = addedItemResult as OkObjectResult;
        return (addedItemObjectResult!.Value! as AddItemSuccessResponse)!;
    }

    public static async Task<AddItemSuccessResponse> AddItemToInventory(TestAppFactory factory, ClaimsPrincipal userClaims, string itemId, int quantity)
    {
        using var controllerPack = CreateControllerPackWithUser<InventoryController>(factory, userClaims);

        return await AddItemToInventory(controllerPack.ControllerInstance, itemId, quantity);
    }

    public static async Task<bool> LockItemAmount(IMediator mediator, string userId, string itemId, int amount)
    {
        var command = new LockItemCommand
        {
            UserId = userId,
            ItemId = itemId,
            Quantity = amount,
            Notify = false
        };

        var lockItemResult = await mediator.Send(command);

        return lockItemResult.Success;
    }

    public static async Task<TradeOfferSuccessResponse> CreateTrade(TradeController controller, string receiverUserId, ItemWithPrice[] tradeItems)
    {
        var request = new TradeOfferRequest
        {
            TargetUserId = receiverUserId,
            Items = tradeItems
        };

        var result = await controller.Offer(request);

        return GetContent<TradeOfferSuccessResponse>(result)!;
    }

    public static async Task<TradeOfferSuccessResponse> CreateTrade(TestAppFactory factory, ClaimsPrincipal userClaims, string receiverUserId, ItemWithPrice[] tradeItems)
    {
        using var controllerPack = CreateControllerPackWithUser<TradeController>(factory, userClaims);

        return await CreateTrade(controllerPack.ControllerInstance, receiverUserId, tradeItems);
    }

    public static async Task<AcceptTradeOfferSuccessResponse> AcceptTrade(TradeController controller, string tradeId)
    {
        var request = new AcceptTradeOfferRequest
        {
            TradeId = tradeId
        };

        var result = await controller.Accept(request);

        var response = GetContent<AcceptTradeOfferSuccessResponse>(result);

        return response!;
    }

    public static async Task<AcceptTradeOfferSuccessResponse> AcceptTrade(TestAppFactory factory, ClaimsPrincipal userClaims, string tradeId)
    {
        var controllerPack = CreateControllerPackWithUser<TradeController>(factory, userClaims);

        return await AcceptTrade(controllerPack.ControllerInstance, tradeId);
    }

    public static async Task<RejectTradeOfferSuccessResponse> RejectTrade(TradeController controller, string tradeId)
    {
        var request = new RejectTradeOfferRequest
        {
            TradeId = tradeId
        };

        var result = await controller.Reject(request);

        return GetContent<RejectTradeOfferSuccessResponse>(result)!;
    }

    public static async Task<RejectTradeOfferSuccessResponse> RejectTrade(TestAppFactory factory, ClaimsPrincipal userClaims, string tradeId)
    {
        var controllerPack = CreateControllerPackWithUser<TradeController>(factory, userClaims);

        return await RejectTrade(controllerPack.ControllerInstance, tradeId);
    }

    public static async Task<CancelTradeOfferSuccessResponse> CancelTrade(TradeController controller, string tradeId)
    {
        var request = new CancelTradeOfferRequest
        {
            TradeId = tradeId
        };

        var result = await controller.Cancel(request);

        return GetContent<CancelTradeOfferSuccessResponse>(result)!;
    }

    public static async Task<CancelTradeOfferSuccessResponse> CancelTrade(TestAppFactory factory, ClaimsPrincipal userClaims, string tradeId)
    {
        var controllerPack = CreateControllerPackWithUser<TradeController>(factory, userClaims);

        return await CancelTrade(controllerPack.ControllerInstance, tradeId);
    }
}