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
using Item_Trading_App_REST_API.Requests.TradeItem;
using MapsterMapper;
using Item_Trading_App_REST_API.Services.UnitOfWork;

namespace Item_Trading_App_REST_API.Services.Trade;

public class TradeService : ITradeService
{
    private readonly DatabaseContext _context;
    private readonly INotificationService _notificationService;
    private readonly ICacheService _cacheService;
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly IUnitOfWorkService _unitOfWork;

    public TradeService(DatabaseContext context, ICacheService cacheService, IMediator mediator, INotificationService notificationService, IMapper mapper, IUnitOfWorkService unitOfWork)
    {
        _context = context;
        _notificationService = notificationService;
        _cacheService = cacheService;
        _mediator = mediator;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<SentTradeOffer> CreateTradeOffer(CreateTradeOffer model)
    {
        if (model is null || string.IsNullOrEmpty(model.SenderUserId) || string.IsNullOrEmpty(model.TargetUserId) || model.Items is null)
            return new SentTradeOffer
            {
                Errors = new[] { "Invalid input data" }
            };

        var items = new List<ItemPrice>();
        Entities.Trade offer;

        _unitOfWork.BeginTransaction();

        try
        {
            foreach (var item in model.Items)
            {
                if (item is null)
                    continue;

                if (item.Price < 0)
                    continue;

                if (!await _mediator.Send(new HasItemQuantityQuery { UserId = model.SenderUserId, ItemId = item.ItemId, Quantity = item.Quantity }))
                    continue;

                if (!(await _mediator.Send(new LockItemQuery { UserId = model.SenderUserId, ItemId = item.ItemId, Quantity = item.Quantity })).Success)
                    continue;

                item.Name = await GetItemNameAsync(item.ItemId);

                items.Add(item);
            }

            if (items.Count == 0)
                return new SentTradeOffer
                {
                    Errors = new[] { "Invalid input data" }
                };

            offer = new Entities.Trade
            {
                TradeId = Guid.NewGuid().ToString(),
                SentDate = DateTime.Now
            };

            await _context.AddEntityAsync(offer);

            foreach(var item in items)
            {
                var request = _mapper.AdaptToType<ItemPrice, AddTradeItemRequest>(item, (nameof(AddTradeItemRequest.TradeId), offer.TradeId));
                if(!await _mediator.Send(request))
                {
                    _unitOfWork.RollbackTransaction();
                    break;
                }
            }

            await _context.SaveChangesAsync();

            await _context.AddEntityAsync(new Entities.SentTrade { TradeId = offer.TradeId, SenderId = model.SenderUserId });

            await _context.AddEntityAsync(new Entities.ReceivedTrade { TradeId = offer.TradeId, ReceiverId = model.TargetUserId });

            _unitOfWork.CommitTransaction();
        }
        catch (Exception)
        {
            _unitOfWork.RollbackTransaction();
            return new SentTradeOffer
            {
                Errors = new[] { "Something went wrong" }
            };
        }

        await _cacheService.SetCacheValueAsync(CacheKeys.Trade.GetTradeKey(offer.TradeId), new CachedTrade
        {
            TradeId = offer.TradeId,
            SenderUserId = model.SenderUserId,
            ReceiverUserId = model.TargetUserId,
            TradeItemsId = items.Select(x => x.ItemId).ToList(),
            SentDate = offer.SentDate
        });

        await _cacheService.SetCacheValueAsync(CacheKeys.Trade.GetSentTradeKey(model.SenderUserId, offer.TradeId), "");
        await _cacheService.SetCacheValueAsync(CacheKeys.Trade.GetReceivedTradeKey(model.TargetUserId, offer.TradeId), "");
        await _notificationService.SendCreatedNotificationToUserAsync(
            model.TargetUserId,
            NotificationCategoryTypes.Trade,
            offer.TradeId);

        return new SentTradeOffer
        {
            TradeOfferId = offer.TradeId,
            ReceiverId = model.TargetUserId,
            ReceiverName = await GetUsernameAsync(model.TargetUserId),
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

        _unitOfWork.BeginTransaction();

        string senderId;

        try
        {
            if (!await _mediator.Send(new TakeCashQuery { UserId = model.UserId, Amount = price }))
            {
                _unitOfWork.RollbackTransaction();
                return new AcceptTradeOfferResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            senderId = await GetSenderId(model.TradeId);
            entity.SenderUserId = senderId;

            if (!await UnlockTradeItemsAsync(senderId, model.TradeId))
            {
                _unitOfWork.RollbackTransaction();
                return new AcceptTradeOfferResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            if (!await GiveItemsAsync(model.UserId, model.TradeId))
            {
                _unitOfWork.RollbackTransaction();
                return new AcceptTradeOfferResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }


            if (!await TakeItemsAsync(senderId, model.TradeId))
            {
                _unitOfWork.RollbackTransaction();
                return new AcceptTradeOfferResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            if (!await _mediator.Send(new GiveCashQuery { UserId = senderId, Amount = price }))
            {
                _unitOfWork.RollbackTransaction();
                return new AcceptTradeOfferResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            entity.Response = true;
            entity.ResponseDate = DateTime.Now;

            await _context.UpdateEntityAsync(new Entities.Trade { TradeId = model.TradeId, Response = entity.Response, ResponseDate = entity.ResponseDate, SentDate = entity.SentDate });
            _unitOfWork.CommitTransaction();
        }
        catch(Exception)
        {
            _unitOfWork.RollbackTransaction();
            return new AcceptTradeOfferResult
            {
                Errors = new[] { "Something went wrong" }
            };
        }

        await _cacheService.SetCacheValueAsync(CacheKeys.Trade.GetTradeKey(model.TradeId), entity);
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
            SenderName = await GetUsernameAsync(senderId),
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

        _unitOfWork.BeginTransaction();

        try
        {
            if (!await UnlockTradeItemsAsync(senderId, model.TradeId))
            {
                _unitOfWork.RollbackTransaction();
                return new RejectTradeOfferResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            trade.Response = false;
            trade.ResponseDate = DateTime.Now;

            await _context.UpdateEntityAsync(new Entities.Trade { TradeId = model.TradeId, Response = trade.Response, ResponseDate = trade.ResponseDate, SentDate = trade.SentDate });
            _unitOfWork.CommitTransaction();
        }
        catch (Exception)
        {
            _unitOfWork.RollbackTransaction();
            return new RejectTradeOfferResult
            {
                Errors = new[] { "Something went wrong" }
            };
        }

        await _cacheService.SetCacheValueAsync(CacheKeys.Trade.GetTradeKey(model.TradeId), trade);
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
            SenderName = await GetUsernameAsync(senderId),
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

        string receiverId;
        _unitOfWork.BeginTransaction();

        try
        {
            if (!await UnlockTradeItemsAsync(model.UserId, model.TradeId))
            {
                _unitOfWork.RollbackTransaction();
                return new CancelTradeOfferResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            receiverId = await GetReceiverId(model.TradeId);

            trade.ReceiverUserId = receiverId;

            if (!await _context.RemoveEntityAsync(new Entities.Trade { TradeId = model.TradeId }))
            {
                _unitOfWork.RollbackTransaction();
                return new CancelTradeOfferResult
                {
                    Errors = new[] { "Something went wrong" } 
                };
            }

            _unitOfWork.CommitTransaction();
        }
        catch (Exception)
        {
            _unitOfWork.RollbackTransaction();
            return new CancelTradeOfferResult
            {
                Errors = new[] { "Something went wrong" }
            };
        }

        await _cacheService.ClearCacheKeyAsync(CacheKeys.Trade.GetTradeKey(model.TradeId));
        await _cacheService.ClearCacheKeyAsync(CacheKeys.Trade.GetSentTradeKey(senderId, model.TradeId));
        await _cacheService.ClearCacheKeyAsync(CacheKeys.Trade.GetReceivedTradeKey(receiverId, model.TradeId));
        trade.TradeItemsId.ForEach(async itemId => await _cacheService.RemoveFromSet(CacheKeys.UsedItem.GetUsedItemKey(itemId), trade.TradeId));
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
            ReceiverName = await GetUsernameAsync(receiverId),
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
            ReceiverName = await GetUsernameAsync(trade.ReceiverUserId),
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
            SenderName = await GetUsernameAsync(trade.SenderUserId),
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
            SenderName = await GetUsernameAsync(senderId),
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
            CacheKeys.Trade.GetSentTradeKey(userId, ""),
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
            CacheKeys.Trade.GetReceivedTradeKey(userId, ""),
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
            CacheKeys.Trade.GetTradeKey(tradeId),
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

    private Task<string> GetUsernameAsync(string userId) => _mediator.Send(new GetUsernameQuery { UserId = userId });

    private Task<List<Models.Trade.TradeItem>> GetTradeItemsAsync(string tradeId) => _mediator.Send(new GetTradeItemsQuery { TradeId = tradeId });

    private Task<List<ItemPrice>> GetItemPricesAsync(string tradeId) => _mediator.Send(new GetItemPricesQuery { TradeId = tradeId });
}
