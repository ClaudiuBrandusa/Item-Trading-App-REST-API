using Item_Trading_App_REST_API.Constants;
using Item_Trading_App_REST_API.Data;
using Item_Trading_App_REST_API.Requests.Wallet;
using Item_Trading_App_REST_API.Models.Item;
using Item_Trading_App_REST_API.Models.Trade;
using Item_Trading_App_REST_API.Requests.Identity;
using Item_Trading_App_REST_API.Requests.Inventory;
using Item_Trading_App_REST_API.Requests.Item;
using Item_Trading_App_REST_API.Services.Cache;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Item_Trading_App_REST_API.Services.Notification;
using Microsoft.EntityFrameworkCore;
using Item_Trading_App_REST_API.Extensions;

namespace Item_Trading_App_REST_API.Services.Trade;

public class TradeService : ITradeService
{
    private readonly DatabaseContext _context;
    private readonly INotificationService _notificationService;
    private readonly ICacheService _cacheService;
    private readonly IMediator _mediator;

    public TradeService(DatabaseContext context, ICacheService cacheService, IMediator mediator, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
        _cacheService = cacheService;
        _mediator = mediator;
    }

    public async Task<SentTradeOffer> CreateTradeOffer(CreateTradeOffer model)
    {
        if (model is null || string.IsNullOrEmpty(model.SenderUserId) || string.IsNullOrEmpty(model.TargetUserId) || model.Items is null)
            return new SentTradeOffer
            {
                Errors = new[] { "Invalid input data" }
            };

        var items = new List<ItemPrice>();

        foreach (var item in model.Items)
        {
            if (item is null)
                continue;

            if (item.Price < 0)
                continue;

            if (!await _mediator.Send(new HasItemQuantityQuery { UserId = model.SenderUserId, ItemId = item.ItemId, Quantity = item.Quantity }))
                continue;

            if (!(await _mediator.Send(new LockItemQuery { UserId = model.SenderUserId, ItemId = item.ItemId, Quantiy = item.Quantity })).Success)
                continue;

            item.Name = await GetItemNameAsync(item.ItemId);

            items.Add(item);
        }

        if (items.Count == 0)
            return new SentTradeOffer
            {
                Errors = new[] { "Invalid input data" }
            };

        var offer = new Entities.Trade
        {
            TradeId = Guid.NewGuid().ToString(),
            SentDate = DateTime.Now
        };

        await _context.AddEntityAsync(offer);

        foreach(var item in items)
        {
            var tradeContent = new Entities.TradeContent
            {
                TradeId = offer.TradeId,
                ItemId = item.ItemId,
                Price = item.Price,
                Quantity = item.Quantity
            };

            _context.TradeContent.Add(tradeContent);
            await _cacheService.SetCacheValueAsync(GetTradeItemCacheKey(offer.TradeId, item.ItemId), item);
        }

        await _context.SaveChangesAsync();

        await _context.AddEntityAsync(new Entities.SentTrade { TradeId = offer.TradeId, SenderId = model.SenderUserId });

        await _context.AddEntityAsync(new Entities.ReceivedTrade { TradeId = offer.TradeId, ReceiverId = model.TargetUserId });

        await _cacheService.SetCacheValueAsync(GetTradesCacheKey(offer.TradeId), new CachedTrade
        {
            TradeId = offer.TradeId,
            SenderUserId = model.SenderUserId,
            ReceiverUserId = model.TargetUserId,
            TradeItemsId = items.Select(x => x.ItemId).ToList(),
            SentDate = offer.SentDate
        });

        await _cacheService.SetCacheValueAsync(GetSentTradesCacheKey(model.SenderUserId, offer.TradeId), "");
        await _cacheService.SetCacheValueAsync(GetReceivedTradesCacheKey(model.TargetUserId, offer.TradeId), "");
        await _notificationService.SendCreatedNotificationToUserAsync(
            model.TargetUserId,
            NotificationCategoryTypes.Trade,
            offer.TradeId);

        return new SentTradeOffer
        {
            TradeOfferId = offer.TradeId,
            ReceiverId = model.TargetUserId,
            ReceiverName = await GetUsername(model.TargetUserId),
            Items = items,
            SentDate = offer.SentDate,
            Success = true
        };
    }

    public async Task<AcceptTradeOfferResult> AcceptTradeOffer(RespondTrade model)
    {
        if (string.IsNullOrEmpty(model.TradeId) || string.IsNullOrEmpty(model.UserId))
            return new AcceptTradeOfferResult
            {
                Errors = new[] { "Invalid IDs" }
            };

        string receiverId = await GetReceiverId(model.TradeId);

        if (!Equals(receiverId, model.UserId))
            return new AcceptTradeOfferResult
            {
                Errors = new[] { "Invalid userId" }
            };

        var entity = await GetCachedTrade(model.TradeId);

        if (entity is null)
            return new AcceptTradeOfferResult
            {
                Errors = new[] { "Something went wrong" }
            };

        if (entity.Response is not null)
            return new AcceptTradeOfferResult
            {
                Errors = new[] { "Already responded" }
            };

        int price = await GetTotalPrice(model.TradeId);

        if (price > await _mediator.Send(new GetUserCashQuery { UserId = model.UserId }))
            return new AcceptTradeOfferResult
            {
                Errors = new[] { "User has not enough money" }
            };

        if (!await _mediator.Send(new TakeCashQuery { UserId = model.UserId, Amount = price }))
            return new AcceptTradeOfferResult
            {
                Errors = new[] { "Something went wrong" }
            };

        string senderId = await GetSenderId(model.TradeId);
        entity.SenderUserId = senderId;

        if (!await UnlockTradeItemsAsync(senderId, model.TradeId))
            return new AcceptTradeOfferResult
            {
                Errors = new[] { "Something went wrong" }
            };

        if (!await GiveItemsAsync(model.UserId, model.TradeId))
            return new AcceptTradeOfferResult
            {
                Errors = new[] { "Something went wrong" }
            };


        if (!await TakeItemsAsync(senderId, model.TradeId))
            return new AcceptTradeOfferResult
            {
                Errors = new[] { "Something went wrong" }
            };

        if (!await _mediator.Send(new GiveCashQuery { UserId = senderId, Amount = price }))
            return new AcceptTradeOfferResult
            {
                Errors = new[] { "Something went wrong" }
            };

        entity.Response = true;
        entity.ResponseDate = DateTime.Now;

        await _context.UpdateEntityAsync(new Entities.Trade { TradeId = model.TradeId, Response = entity.Response, ResponseDate = entity.ResponseDate, SentDate = entity.SentDate });
        
        await _cacheService.SetCacheValueAsync(GetTradesCacheKey(model.TradeId), entity);
        await _notificationService.SendUpdatedNotificationToUserAsync(
            senderId,
            NotificationCategoryTypes.Trade,
            model.TradeId,
            new RespondedTradeNotification
            {
                Response = entity.Response
            });

        return new AcceptTradeOfferResult
        {
            TradeOfferId = model.TradeId,
            SenderId = senderId,
            SenderName = await GetUsername(senderId),
            ReceivedDate = entity.SentDate,
            ResponseDate = (DateTime)entity.ResponseDate,
            Success = true
        };
    }

    public async Task<RejectTradeOfferResult> RejectTradeOffer(RespondTrade model)
    {
        if (string.IsNullOrEmpty(model.TradeId) || string.IsNullOrEmpty(model.UserId))
            return new RejectTradeOfferResult
            {
                Errors = new[] { "Invalid IDs" }
            };

        string receiverId = await GetReceiverId(model.TradeId);

        if (!Equals(receiverId, model.UserId))
            return new RejectTradeOfferResult
            {
                Errors = new[] { "Invalid userId" }
            };

        var trade = await GetCachedTrade(model.TradeId);

        if (trade is null)
            return new RejectTradeOfferResult
            {
                Errors = new[] { "Something went wrong" }
            };

        if (trade.Response is not null)
            return new RejectTradeOfferResult
            {
                Errors = new[] { "Already responded" }
            };

        string senderId = await GetSenderId(model.TradeId);
        trade.SenderUserId = senderId;

        if (!await UnlockTradeItemsAsync(senderId, model.TradeId))
            return new RejectTradeOfferResult
            {
                Errors = new[] { "Something went wrong" }
            };

        trade.Response = false;
        trade.ResponseDate = DateTime.Now;

        await _context.UpdateEntityAsync(new Entities.Trade { TradeId = model.TradeId, Response = trade.Response, ResponseDate = trade.ResponseDate, SentDate = trade.SentDate });
        
        await _cacheService.SetCacheValueAsync(GetTradesCacheKey(model.TradeId), trade);
        await _notificationService.SendUpdatedNotificationToUserAsync(
            senderId,
            NotificationCategoryTypes.Trade,
            model.TradeId,
            new RespondedTradeNotification
            {
                Response = trade.Response
            });

        return new RejectTradeOfferResult
        {
            TradeOfferId = model.TradeId,
            SenderId = senderId,
            SenderName = await GetUsername(senderId),
            ReceivedDate = trade.SentDate,
            ResponseDate = (DateTime)trade.ResponseDate,
            Success = true
        };
    }

    public async Task<CancelTradeOfferResult> CancelTradeOffer(RespondTrade model)
    {
        if (string.IsNullOrEmpty(model.TradeId) || string.IsNullOrEmpty(model.UserId))
            return new CancelTradeOfferResult
            {
                Errors = new[] { "Invalid IDs" }
            };

        string senderId = await GetSenderId(model.TradeId);

        if (!Equals(senderId, model.UserId))
            return new CancelTradeOfferResult
            {
                Errors = new[] { "Invalid userId" }
            };

        var trade = await GetCachedTrade(model.TradeId);

        if (trade is null)
            return new CancelTradeOfferResult
            {
                Errors = new[] { "Something went wrong" }
            };

        if (trade.Response.HasValue)
            return new CancelTradeOfferResult
            {
                Errors = new[] { "Unable to cancel a trade that already got a response" }
            };

        if (!await UnlockTradeItemsAsync(model.UserId, model.TradeId))
            return new CancelTradeOfferResult
            {
                Errors = new[] { "Something went wrong" }
            };

        string receiverId = await GetReceiverId(model.TradeId);

        trade.ReceiverUserId = receiverId;

        if (!await _context.RemoveEntityAsync(new Entities.Trade { TradeId = model.TradeId }))
            return new CancelTradeOfferResult
            {
                Errors = new[] { "Something went wrong" } 
            };

        await _cacheService.ClearCacheKeyAsync(GetTradesCacheKey(model.TradeId));
        await _cacheService.ClearCacheKeyAsync(GetSentTradesCacheKey(senderId, model.TradeId));
        await _cacheService.ClearCacheKeyAsync(GetReceivedTradesCacheKey(receiverId, model.TradeId));
        await _notificationService.SendUpdatedNotificationToUserAsync(
            receiverId,
            NotificationCategoryTypes.Trade,
            model.TradeId, new RespondedTradeNotification
            {
                Response = trade.Response
            });

        return new CancelTradeOfferResult
        {
            TradeOfferId = model.TradeId,
            ReceiverId = receiverId,
            ReceiverName = await GetUsername(receiverId),
            Success = true
        };
    }

    public async Task<TradeOffersResult> GetReceivedTradeOffers(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return new TradeOffersResult
            {
                Errors = new[] { "Invalid user ID" }
            };

        var idList = await GetReceivedTradeOffersIdList(userId);

        if (idList is null)
            return new TradeOffersResult
            {
                Errors = new[] { "Something went wrong" }
            };

        return new TradeOffersResult
        {
            TradeOffers = idList,
            Success = true
        };
    }

    public async Task<TradeOffersResult> GetReceivedRespondedTradeOffers(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return new TradeOffersResult
            {
                Errors = new[] { "Invalid user ID" }
            };

        var idList = await GetReceivedTradeOffersIdList(userId, true);

        if (idList is null)
            return new TradeOffersResult
            {
                Errors = new[] { "Something went wrong" }
            };

        return new TradeOffersResult
        {
            TradeOffers = idList,
            Success = true
        };
    }

    public async Task<TradeOffersResult> GetSentTradeOffers(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return new TradeOffersResult
            {
                Errors = new[] { "Invalid user ID" }
            };

        var idList = await GetSentTradeOffersIdList(userId);

        if (idList is null)
            return new TradeOffersResult
            {
                Errors = new[] { "Something went wrong" }
            };

        return new TradeOffersResult
        {
            TradeOffers = idList,
            Success = true
        };
    }

    public async Task<TradeOffersResult> GetSentRespondedTradeOffers(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return new TradeOffersResult
            {
                Errors = new[] { "" }
            };

        var idList = await GetSentTradeOffersIdList(userId, true);

        if (idList is null)
            return new TradeOffersResult
            {
                Errors = new[] { "Something went wrong" }
            };

        return new TradeOffersResult
        {
            TradeOffers = idList,
            Success = true
        };
    }

    public async Task<SentTradeOffer> GetSentTradeOffer(RequestTradeOffer requestTradeOffer)
    {
        if (requestTradeOffer is null || string.IsNullOrEmpty(requestTradeOffer.TradeOfferId) || string.IsNullOrEmpty(requestTradeOffer.UserId))
            return new SentTradeOffer
            {
                Errors = new[] { "Invalid input data" }
            };

        var trade = await GetCachedTrade(requestTradeOffer.TradeOfferId);

        if (!Equals(requestTradeOffer.UserId, trade.SenderUserId))
            return new SentTradeOffer
            {
                Errors = new[] { "User has not sent this trade offer" }
            };

        return new SentTradeOffer
        {
            TradeOfferId = requestTradeOffer.TradeOfferId,
            ReceiverId = trade.ReceiverUserId,
            ReceiverName = await GetUsername(trade.ReceiverUserId),
            SentDate = trade.SentDate,
            Items = await GetItemPricesAsync(requestTradeOffer.TradeOfferId),
            Success = true
        };
    }

    public async Task<SentRespondedTradeOffer> GetSentRespondedTradeOffer(RequestTradeOffer requestTradeOffer)
    {
        if (requestTradeOffer is null || string.IsNullOrEmpty(requestTradeOffer.TradeOfferId) || string.IsNullOrEmpty(requestTradeOffer.UserId))
            return new SentRespondedTradeOffer
            {
                Errors = new[] { "Invalid input data" }
            };

        var entity = await GetSentTradeOffer(requestTradeOffer);

        if (!entity.Success)
            return new SentRespondedTradeOffer
            {
                Errors = entity.Errors
            };

        var response = await GetTradeResponseAsync(entity.TradeOfferId);
        var responseDate = await GetTradeResponseDateAsync(entity.TradeOfferId);

        if (response is null || responseDate is null)
            return new SentRespondedTradeOffer
            {
                Errors = new[] { "Something went wrong" }
            };

        return new SentRespondedTradeOffer
        {
            TradeOfferId = requestTradeOffer.TradeOfferId,
            ReceiverId = entity.ReceiverId,
            ReceiverName = entity.ReceiverName,
            Response = (bool)response,
            ResponseDate = (DateTime)responseDate,
            SentDate = entity.SentDate,
            Items = entity.Items,
            Success = true
        };
    }

    public async Task<ReceivedTradeOffer> GetReceivedTradeOffer(RequestTradeOffer requestTradeOffer)
    {
        if (requestTradeOffer is null || string.IsNullOrEmpty(requestTradeOffer.TradeOfferId) || string.IsNullOrEmpty(requestTradeOffer.UserId))
            return new ReceivedTradeOffer
            {
                Errors = new[] { "Invalid input data" }
            };

        var trade = await GetCachedTrade(requestTradeOffer.TradeOfferId);
        
        if (!Equals(requestTradeOffer.UserId, trade.ReceiverUserId))
            return new ReceivedTradeOffer
            {
                Errors = new[] { "User has not sent this trade offer" }
            };

        if (trade is null)
            return new ReceivedTradeOffer
            {
                Errors = new[] { "Something went wrong" }
            };

        return new ReceivedTradeOffer
        {
            TradeOfferId = requestTradeOffer.TradeOfferId,
            SenderId = trade.SenderUserId,
            SenderName = await GetUsername(trade.SenderUserId),
            SentDate = trade.SentDate,
            Items = await GetItemPricesAsync(requestTradeOffer.TradeOfferId),
            Success = true
        };
    }

    public async Task<ReceivedRespondedTradeOffer> GetReceivedRespondedTradeOffer(RequestTradeOffer requestTradeOffer)
    {
        if (requestTradeOffer is null || string.IsNullOrEmpty(requestTradeOffer.TradeOfferId) || string.IsNullOrEmpty(requestTradeOffer.UserId))
            return new ReceivedRespondedTradeOffer
            {
                Errors = new[] { "Invalid input data" }
            };

        var entity = await GetReceivedTradeOffer(requestTradeOffer);

        if (!entity.Success)
            return new ReceivedRespondedTradeOffer
            {
                Errors = entity.Errors
            };

        var response = await GetTradeResponseAsync(entity.TradeOfferId);
        var responseDate = await GetTradeResponseDateAsync(entity.TradeOfferId);

        if (response is null || responseDate is null)
            return new ReceivedRespondedTradeOffer
            {
                Errors = new[] { "Something went wrong" }
            };

        string senderId = await GetSenderId(requestTradeOffer.TradeOfferId);

        return new ReceivedRespondedTradeOffer
        {
            TradeOfferId = requestTradeOffer.TradeOfferId,
            SenderId = senderId,
            SenderName = await GetUsername(senderId),
            Response = (bool)response,
            ResponseDate = (DateTime)responseDate,
            SentDate = entity.SentDate,
            Items = await GetItemPricesAsync(requestTradeOffer.TradeOfferId),
            Success = true
        };
    }

    private async Task<List<string>> GetSentTradeOffersIdList(string userId, bool responded = false)
    {
        var list = await _cacheService.GetEntityIdsAsync(
            GetSentTradesCacheKey(userId, ""),
            async (args) => await _context.SentTrades
                .AsNoTracking()
                .Where(st => Equals(st.SenderId, userId))
                .Select(t => t.TradeId)
                .ToListAsync(),
            true);

        return await FilterTradeOffers(list, responded);
    }

    private async Task<List<string>> GetReceivedTradeOffersIdList(string userId, bool responded = false)
    {
        var list = await _cacheService.GetEntityIdsAsync(
            GetReceivedTradesCacheKey(userId, ""),
            async (args) => await _context.ReceivedTrades
                .AsNoTracking()
                .Where(o => Equals(userId, o.ReceiverId))
                .Select(t => t.TradeId)
                .ToListAsync(),
            true);

        return await FilterTradeOffers(list, responded);
    }

    private async Task<List<string>> FilterTradeOffers(List<string> tradeOffersList, bool responded = false)
    {
        return await tradeOffersList
            .ToAsyncEnumerable()
            .WhereAwait(async x => await IsResponded(x) == responded)
            .ToListAsync();
    }

    private async Task<bool> IsResponded(string tradeId) => (await GetTradeResponseAsync(tradeId)) is not null;

    private Task<List<ItemPrice>> GetItemPricesAsync(string tradeId)
    {
        return _cacheService.GetEntitiesAsync(GetTradeItemCacheKey(tradeId, ""), async (args) =>
        {
            return await _context.TradeContent.AsNoTracking().Where(t => Equals(t.TradeId, tradeId)).ToListAsync();
        }, async (content) =>
        {
            return new ItemPrice
            {
                ItemId = content.ItemId,
                Price = content.Price,
                Name = await GetItemNameAsync(content.ItemId),
                Quantity = content.Quantity
            };
        }, true, (ItemPrice itemPrice) => itemPrice.ItemId); ;
    }

    private async Task<string> GetReceiverId(string tradeId)
    {
        var trade = await GetCachedTrade(tradeId);

        return trade?.ReceiverUserId ?? string.Empty;
    }

    private async Task<string> GetSenderId(string tradeId)
    {
        var trade = await GetCachedTrade(tradeId);

        return trade?.SenderUserId ?? string.Empty;
    }

    private Task<CachedTrade> GetCachedTrade(string tradeId)
    {
        return _cacheService.GetEntityAsync(
            GetTradesCacheKey(tradeId),
            async (args) =>
            {
                var trade = _context.Trades.AsNoTracking().FirstOrDefault(t => Equals(t.TradeId, tradeId));
                var sentTrade = _context.SentTrades.AsNoTracking().FirstOrDefault(t => Equals(t.TradeId, tradeId));
                var receivedTrade = _context.ReceivedTrades.AsNoTracking().FirstOrDefault(t => Equals(t.TradeId, tradeId));

                return new CachedTrade
                {
                    TradeId = trade.TradeId,
                    SenderUserId = sentTrade.SenderId,
                    ReceiverUserId = receivedTrade.ReceiverId,
                    SentDate = trade.SentDate,
                    Response = trade.Response,
                    ResponseDate = trade.ResponseDate,
                    TradeItemsId = (await GetTradeItemsAsync(trade.TradeId)).Select(x => x.ItemId).ToList()
                };
            },
            true);
    }

    /// <returns>null -> trade has no response<br/> 
    /// true -> trade has 'Accepted' as response<br/>
    /// false -> trade has 'Declined' as response</returns>
    private async Task<bool?> GetTradeResponseAsync(string tradeId)
    {
        var trade = await GetCachedTrade(tradeId);

        return trade?.Response;
    }

    private async Task<DateTime?> GetTradeResponseDateAsync(string tradeId)
    {
        var trade = await GetCachedTrade(tradeId);

        return trade?.ResponseDate;
    }

    private async Task<List<TradeItem>> GetTradeItemsAsync(string tradeId)
    {
        var tradeContents = await GetItemPricesAsync(tradeId);

        var results = new List<TradeItem>();

        foreach (var content in tradeContents)
        {
            if (content is null)
                continue;

            results.Add(new TradeItem
            {
                ItemId = content.ItemId,
                Quantity = content.Quantity
            });
        }

        return results;
    }

    private async Task<bool> UnlockTradeItemsAsync(string userId, string tradeId)
    {
        var tradeItems = await GetTradeItemsAsync(tradeId);

        foreach (var item in tradeItems)
        {
            if (item is null)
                continue;

            await _mediator.Send(new UnlockItemQuery { UserId = userId, ItemId = item.ItemId, Quantity = item.Quantity });
        }

        return tradeItems.Count != 0;
    }

    // Takes the items from trade to the receiver
    private async Task<bool> GiveItemsAsync(string userId, string tradeId)
    {
        var tradeItems = await GetTradeItemsAsync(tradeId);

        foreach (var item in tradeItems)
            await _mediator.Send(new AddItemQuery { UserId = userId, ItemId = item.ItemId, Quantity = item.Quantity });

        return tradeItems.Count != 0;
    }

    // Takes the items from the sender
    private async Task<bool> TakeItemsAsync(string userId, string tradeId)
    {
        var tradeItems = await GetTradeItemsAsync(tradeId);

        foreach (var item in tradeItems)
            await _mediator.Send(new DropItemQuery { UserId = userId, ItemId = item.ItemId, Quantity = item.Quantity });

        return tradeItems.Count != 0;
    }

    private async Task<int> GetTotalPrice(string tradeOfferId)
    {
        var list = await GetItemPricesAsync(tradeOfferId);

        if (list is null || list.Count == 0)
            return 0;

        int total = 0;

        foreach (var item in list)
            total += item.Price;

        return total;
    }

    private Task<string> GetItemNameAsync(string itemId) => _mediator.Send(new GetItemNameQuery { ItemId = itemId });

    private Task<string> GetUsername(string userId) => _mediator.Send(new GetUsernameQuery { UserId = userId });

    private static string GetTradesCacheKey(string tradeId) => CachePrefixKeys.Trades + CachePrefixKeys.Trade + tradeId;

    private static string GetSentTradesCacheKey(string userId, string tradeId) => CachePrefixKeys.Trades + CachePrefixKeys.SentTrades + userId + "+" + tradeId;

    private static string GetReceivedTradesCacheKey(string userId, string tradeId) => CachePrefixKeys.Trades + CachePrefixKeys.ReceivedTrades + userId + "+" + tradeId;

    private static string GetTradeItemCacheKey(string tradeId, string itemId) => CachePrefixKeys.Trades + tradeId + ":" + CachePrefixKeys.TradeItem + itemId;
}
