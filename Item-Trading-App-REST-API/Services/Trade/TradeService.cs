using Item_Trading_App_REST_API.Constants;
using Item_Trading_App_REST_API.Data;
using Item_Trading_App_REST_API.Resources.Queries.Wallet;
using Item_Trading_App_REST_API.Models.Trade;
using Item_Trading_App_REST_API.Resources.Queries.Identity;
using Item_Trading_App_REST_API.Resources.Queries.Inventory;
using Item_Trading_App_REST_API.Resources.Queries.Item;
using Item_Trading_App_REST_API.Services.Cache;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Item_Trading_App_REST_API.Extensions;
using MapsterMapper;
using Item_Trading_App_REST_API.Services.UnitOfWork;
using Item_Trading_App_REST_API.Resources.Queries.TradeItem;
using Item_Trading_App_REST_API.Resources.Commands.Inventory;
using Item_Trading_App_REST_API.Resources.Commands.Wallet;
using Item_Trading_App_REST_API.Resources.Queries.Trade;
using Item_Trading_App_REST_API.Resources.Commands.Trade;
using Item_Trading_App_REST_API.Resources.Commands.TradeItem;
using Item_Trading_App_REST_API.Models.Base;
using Item_Trading_App_REST_API.Resources.Commands.TradeItemHistory;
using Item_Trading_App_REST_API.Resources.Queries.TradeItemHistory;
using Item_Trading_App_REST_API.Resources.Events.Trades;

namespace Item_Trading_App_REST_API.Services.Trade;

public class TradeService : ITradeService
{
    private readonly DatabaseContext _context;
    private readonly ICacheService _cacheService;
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly IUnitOfWorkService _unitOfWork;

    public TradeService(DatabaseContext context, ICacheService cacheService, IMediator mediator, IMapper mapper, IUnitOfWorkService unitOfWork)
    {
        _context = context;
        _cacheService = cacheService;
        _mediator = mediator;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<SentTradeOfferResult> CreateTradeOfferAsync(CreateTradeOfferCommand model)
    {
        if (model is null || string.IsNullOrEmpty(model.SenderUserId) || string.IsNullOrEmpty(model.TargetUserId) || model.Items is null)
            return new SentTradeOfferResult
            {
                Errors = new[] { "Invalid input data" }
            };

        var items = new List<Models.TradeItems.TradeItem>();
        Entities.Trade offer;

        _unitOfWork.BeginTransaction();

        try
        {
            await ProcessTradeItemsFromInputModelAsync(model, items);

            if (items.Count == 0)
                return new SentTradeOfferResult
                {
                    Errors = new[] { "Invalid input data" }
                };

            offer = new Entities.Trade
            {
                TradeId = Guid.NewGuid().ToString(),
                SentDate = DateTime.Now
            };

            await _context.AddEntityAsync(offer);

            foreach (var item in items)
            {
                var request = _mapper.AdaptToType<Models.TradeItems.TradeItem, AddTradeItemCommand>(item, ((string, object))(nameof(AddTradeItemCommand.TradeId), offer.TradeId));
                if (!await _mediator.Send(request))
                {
                    _unitOfWork.RollbackTransaction();
                    break;
                }
            }

            await AddSentAndReceivedTradeEntitiesAsync(offer.TradeId, model.SenderUserId, model.TargetUserId);

            await _context.SaveChangesAsync();

            _unitOfWork.CommitTransaction();
        }
        catch (Exception)
        {
            _unitOfWork.RollbackTransaction();
            return new SentTradeOfferResult
            {
                Errors = new[] { "Something went wrong" }
            };
        }

        await SetCacheForCreatedTradeAsync(offer, model);

        var usernameTask = GetUsernameAsync(model.TargetUserId);

        await _mediator.Publish(new TradeCreatedEvent
        {
            TradeId = offer.TradeId,
            ReceiverId = model.TargetUserId,
        });

        return new SentTradeOfferResult
        {
            TradeOfferId = offer.TradeId,
            ReceiverId = model.TargetUserId,
            ReceiverName = await usernameTask,
            Items = items,
            SentDate = offer.SentDate,
            Success = true
        };
    }

    public async Task<RespondedTradeOfferResult> AcceptTradeOfferAsync(RespondTradeCommand model)
    {
        if (string.IsNullOrEmpty(model.TradeId) || string.IsNullOrEmpty(model.UserId))
            return new RespondedTradeOfferResult
            {
                Errors = new[] { "Invalid IDs" }
            };

        string receiverId = await GetReceiverIdAsync(model.TradeId);

        if (!Equals(receiverId, model.UserId))
            return new RespondedTradeOfferResult
            {
                Errors = new[] { "Invalid userId" }
            };

        var trade = await GetCachedTradeAsync(model.TradeId);

        if (trade is null)
            return new RespondedTradeOfferResult
            {
                Errors = new[] { "Something went wrong" }
            };

        if (trade.Response is not null)
            return new RespondedTradeOfferResult
            {
                Errors = new[] { "Already responded" }
            };

        int price = await GetTotalPriceAsync(model.TradeId);

        if (price > await _mediator.Send(new GetUserCashQuery { UserId = model.UserId }))
            return new RespondedTradeOfferResult
            {
                Errors = new[] { "User has not enough money" }
            };

        _unitOfWork.BeginTransaction();

        string senderId;

        try
        {
            if (!await _mediator.Send(new TakeCashCommand { UserId = model.UserId, Amount = price }))
            {
                _unitOfWork.RollbackTransaction();
                return new RespondedTradeOfferResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            senderId = await GetSenderIdAsync(model.TradeId);
            trade.SenderUserId = senderId;

            if (!await UnlockTradeItemsAsync(senderId, model.TradeId))
            {
                _unitOfWork.RollbackTransaction();
                return new RespondedTradeOfferResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            if (!await GiveItemsAsync(model.UserId, model.TradeId))
            {
                _unitOfWork.RollbackTransaction();
                return new RespondedTradeOfferResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            if (!await TakeItemsAsync(senderId, model.TradeId))
            {
                _unitOfWork.RollbackTransaction();
                return new RespondedTradeOfferResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            if (!await _mediator.Send(new GiveCashCommand { UserId = senderId, Amount = price }))
            {
                _unitOfWork.RollbackTransaction();
                return new RespondedTradeOfferResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            var respondTradeResult = await RespondTradeAsync(model);

            if (!respondTradeResult.Success)
            {
                _unitOfWork.RollbackTransaction();
                return new RespondedTradeOfferResult
                {
                    Errors = respondTradeResult.Errors
                };
            }

            await UpdateTradeEntityAsync(trade, true);

            _unitOfWork.CommitTransaction();
        }
        catch (Exception)
        {
            _unitOfWork.RollbackTransaction();
            return new RespondedTradeOfferResult
            {
                Errors = new[] { "Something went wrong" }
            };
        }

        await _cacheService.SetCacheValueAsync(CacheKeys.Trade.GetTradeKey(model.TradeId), trade);

        var usernameTask = GetUsernameAsync(senderId);

        await _mediator.Publish(new TradeRespondedEvent
        {
            TradeId = model.TradeId,
            SenderId = senderId,
            Response = true
        });

        return new RespondedTradeOfferResult
        {
            TradeOfferId = model.TradeId,
            SenderId = senderId,
            SenderName = await usernameTask,
            ReceivedDate = trade.SentDate,
            ResponseDate = (DateTime)trade.ResponseDate,
            Success = true
        };
    }

    public async Task<RespondedTradeOfferResult> RejectTradeOfferAsync(RespondTradeCommand model)
    {
        if (string.IsNullOrEmpty(model.TradeId) || string.IsNullOrEmpty(model.UserId))
            return new RespondedTradeOfferResult
            {
                Errors = new[] { "Invalid IDs" }
            };

        string receiverId = await GetReceiverIdAsync(model.TradeId);

        if (!Equals(receiverId, model.UserId))
            return new RespondedTradeOfferResult
            {
                Errors = new[] { "Invalid userId" }
            };

        var trade = await GetCachedTradeAsync(model.TradeId);

        if (trade is null)
            return new RespondedTradeOfferResult
            {
                Errors = new[] { "Something went wrong" }
            };

        if (trade.Response is not null)
            return new RespondedTradeOfferResult
            {
                Errors = new[] { "Already responded" }
            };

        string senderId = await GetSenderIdAsync(model.TradeId);
        trade.SenderUserId = senderId;

        _unitOfWork.BeginTransaction();

        try
        {
            if (!await UnlockTradeItemsAsync(senderId, model.TradeId))
            {
                _unitOfWork.RollbackTransaction();
                return new RespondedTradeOfferResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            var respondTradeResult = await RespondTradeAsync(model);

            if (!respondTradeResult.Success)
            {
                _unitOfWork.RollbackTransaction();
                return new RespondedTradeOfferResult
                {
                    Errors = respondTradeResult.Errors
                };
            }

            await UpdateTradeEntityAsync(trade, false);

            _unitOfWork.CommitTransaction();
        }
        catch (Exception)
        {
            _unitOfWork.RollbackTransaction();
            return new RespondedTradeOfferResult
            {
                Errors = new[] { "Something went wrong" }
            };
        }

        await _cacheService.SetCacheValueAsync(CacheKeys.Trade.GetTradeKey(model.TradeId), trade);

        var usernameTask = GetUsernameAsync(senderId);

        await _mediator.Publish(new TradeRespondedEvent
        {
            TradeId = model.TradeId,
            SenderId = senderId,
            Response = false
        });

        return new RespondedTradeOfferResult
        {
            TradeOfferId = model.TradeId,
            SenderId = senderId,
            SenderName = await usernameTask,
            ReceivedDate = trade.SentDate,
            ResponseDate = (DateTime)trade.ResponseDate,
            Success = true
        };
    }

    public async Task<CancelTradeOfferResult> CancelTradeOfferAsync(CancelTradeCommand model)
    {
        if (string.IsNullOrEmpty(model.TradeId) || string.IsNullOrEmpty(model.UserId))
            return new CancelTradeOfferResult
            {
                Errors = new[] { "Invalid IDs" }
            };

        string senderId = await GetSenderIdAsync(model.TradeId);

        if (!Equals(senderId, model.UserId))
            return new CancelTradeOfferResult
            {
                Errors = new[] { "Invalid userId" }
            };

        var trade = await GetCachedTradeAsync(model.TradeId);

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

            receiverId = await GetReceiverIdAsync(model.TradeId);
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

        await ClearCacheUsedForTradeAsync(model.TradeId, senderId, receiverId, trade.TradeItemsId);

        var usernameTask = GetUsernameAsync(receiverId);

        await _mediator.Publish(new TradeCancelledEvent
        {
            TradeId = model.TradeId,
            ReceiverId = receiverId
        });

        return new CancelTradeOfferResult
        {
            TradeOfferId = model.TradeId,
            ReceiverId = receiverId,
            ReceiverName = await usernameTask,
            Success = true
        };
    }

    public async Task<TradeOffersResult> GetReceivedTradeOffersAsync(ListTradesQuery model)
    {
        if (string.IsNullOrEmpty(model.UserId))
            return new TradeOffersResult
            {
                Errors = new[] { "Invalid input data" }
            };

        var idList = await GetReceivedTradeOffersIdListAsync(model.UserId, model.TradeItemIds);

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

    public async Task<TradeOffersResult> GetReceivedRespondedTradeOffersAsync(ListTradesQuery model)
    {
        if (string.IsNullOrEmpty(model.UserId))
            return new TradeOffersResult
            {
                Errors = new[] { "Invalid input data" }
            };

        var idList = await GetReceivedTradeOffersIdListAsync(model.UserId, model.TradeItemIds, true);

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

    public async Task<TradeOffersResult> GetSentTradeOffersAsync(ListTradesQuery model)
    {
        if (string.IsNullOrEmpty(model.UserId))
            return new TradeOffersResult
            {
                Errors = new[] { "Invalid input data" }
            };

        var idList = await GetSentTradeOffersIdListAsync(model.UserId, model.TradeItemIds);

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

    public async Task<TradeOffersResult> GetSentRespondedTradeOffersAsync(ListTradesQuery model)
    {
        if (string.IsNullOrEmpty(model.UserId))
            return new TradeOffersResult
            {
                Errors = new[] { "Invalid input data" }
            };

        var idList = await GetSentTradeOffersIdListAsync(model.UserId, model.TradeItemIds, true);

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

    public async Task<SentTradeOfferResult> GetSentTradeOfferAsync(RequestTradeOfferQuery requestTradeOffer)
    {
        if (requestTradeOffer is null || string.IsNullOrEmpty(requestTradeOffer.TradeOfferId) || string.IsNullOrEmpty(requestTradeOffer.UserId))
            return new SentTradeOfferResult
            {
                Errors = new[] { "Invalid input data" }
            };

        var trade = await GetCachedTradeAsync(requestTradeOffer.TradeOfferId);

        if (!Equals(requestTradeOffer.UserId, trade.SenderUserId))
            return new SentTradeOfferResult
            {
                Errors = new[] { "User has not sent this trade offer" }
            };

        var usernameTask = GetUsernameAsync(trade.ReceiverUserId);
        var tradeItemsTask = GetTradeItemsAsync(requestTradeOffer.TradeOfferId, false);

        await Task.WhenAll(
            usernameTask,
            tradeItemsTask
        );

        string receiverName = usernameTask.Result;
        var tradeItems = tradeItemsTask.Result;

        return new SentTradeOfferResult
        {
            TradeOfferId = trade.TradeId,
            ReceiverId = trade.ReceiverUserId,
            ReceiverName = receiverName,
            SentDate = trade.SentDate,
            Items = tradeItems,
            Success = true
        };
    }

    public async Task<RespondedSentTradeOfferResult> GetRespondedSentTradeOfferAsync(RequestTradeOfferQuery requestTradeOffer)
    {
        if (requestTradeOffer is null || string.IsNullOrEmpty(requestTradeOffer.TradeOfferId) || string.IsNullOrEmpty(requestTradeOffer.UserId))
            return new RespondedSentTradeOfferResult
            {
                Errors = new[] { "Invalid input data" }
            };

        var trade = await GetCachedTradeAsync(requestTradeOffer.TradeOfferId);

        if (!Equals(requestTradeOffer.UserId, trade.SenderUserId))
            return new RespondedSentTradeOfferResult
            {
                Errors = new[] { "User has not sent this trade offer" }
            };

        var responseTask = GetTradeResponseAsync(trade.TradeId);
        var tradeResponseDateTask = GetTradeResponseDateAsync(trade.TradeId);

        await Task.WhenAll(
            responseTask,
            tradeResponseDateTask
        );

        var response = responseTask.Result;
        var responseDate = tradeResponseDateTask.Result;

        if (response is null || responseDate is null)
            return new RespondedSentTradeOfferResult
            {
                Errors = new[] { "Something went wrong" }
            };

        var usernameTask = GetUsernameAsync(trade.SenderUserId);
        var tradeItemsTask = GetTradeItemsAsync(requestTradeOffer.TradeOfferId, false);

        await Task.WhenAll(
            usernameTask,
            tradeItemsTask
        );

        var senderName = usernameTask.Result;
        var tradeItems = tradeItemsTask.Result;

        return new RespondedSentTradeOfferResult
        {
            TradeOfferId = requestTradeOffer.TradeOfferId,
            ReceiverId = trade.ReceiverUserId,
            ReceiverName = senderName,
            Response = (bool)response,
            ResponseDate = (DateTime)responseDate,
            SentDate = trade.SentDate,
            Items = tradeItems,
            Success = true
        };
    }

    public async Task<ReceivedTradeOfferResult> GetReceivedTradeOfferAsync(RequestTradeOfferQuery requestTradeOffer)
    {
        if (requestTradeOffer is null || string.IsNullOrEmpty(requestTradeOffer.TradeOfferId) || string.IsNullOrEmpty(requestTradeOffer.UserId))
            return new ReceivedTradeOfferResult
            {
                Errors = new[] { "Invalid input data" }
            };

        var trade = await GetCachedTradeAsync(requestTradeOffer.TradeOfferId);

        if (!Equals(requestTradeOffer.UserId, trade.ReceiverUserId))
            return new ReceivedTradeOfferResult
            {
                Errors = new[] { "User has not sent this trade offer" }
            };

        var usernameTask = GetUsernameAsync(trade.SenderUserId);
        var tradeItemsTask = GetTradeItemsAsync(requestTradeOffer.TradeOfferId, false);

        await Task.WhenAll(
            usernameTask,
            tradeItemsTask
        );

        var senderName = usernameTask.Result;
        var tradeItems = tradeItemsTask.Result;

        return new ReceivedTradeOfferResult
        {
            TradeOfferId = trade.TradeId,
            SenderId = trade.SenderUserId,
            SenderName = senderName,
            SentDate = trade.SentDate,
            Items = tradeItems,
            Success = true
        };
    }

    public async Task<RespondedReceivedTradeOfferResult> GetReceivedRespondedTradeOfferAsync(RequestTradeOfferQuery requestTradeOffer)
    {
        if (requestTradeOffer is null || string.IsNullOrEmpty(requestTradeOffer.TradeOfferId) || string.IsNullOrEmpty(requestTradeOffer.UserId))
            return new RespondedReceivedTradeOfferResult
            {
                Errors = new[] { "Invalid input data" }
            };

        var trade = await GetCachedTradeAsync(requestTradeOffer.TradeOfferId);

        if (!Equals(requestTradeOffer.UserId, trade.ReceiverUserId))
            return new RespondedReceivedTradeOfferResult
            {
                Errors = new[] { "User has not sent this trade offer" }
            };

        var tradeResponseTask = GetTradeResponseAsync(trade.TradeId);
        var tradeResponseDateTask = GetTradeResponseDateAsync(trade.TradeId);

        await Task.WhenAll(
            tradeResponseTask,
            tradeResponseDateTask
        );

        var response = tradeResponseTask.Result;
        var responseDate = tradeResponseDateTask.Result;

        if (response is null || responseDate is null)
            return new RespondedReceivedTradeOfferResult
            {
                Errors = new[] { "Something went wrong" }
            };

        var senderIdTask = GetSenderIdAsync(requestTradeOffer.TradeOfferId);
        var tradeItemsTask = GetTradeItemsAsync(requestTradeOffer.TradeOfferId, true);

        await Task.WhenAll(
            senderIdTask,
            tradeItemsTask
        );

        var senderId = senderIdTask.Result;
        var senderName = await GetUsernameAsync(senderId);
        var tradeItems = tradeItemsTask.Result;

        return new RespondedReceivedTradeOfferResult
        {
            TradeOfferId = requestTradeOffer.TradeOfferId,
            SenderId = senderId,
            SenderName = senderName,
            Response = (bool)response,
            ResponseDate = (DateTime)responseDate,
            SentDate = trade.SentDate,
            Items = tradeItems,
            Success = true
        };
    }

    private async Task<BaseResult> RespondTradeAsync(RespondTradeCommand model)
    {
        // get trade items
        var tradeItems = await _mediator.Send(new GetTradeItemsQuery { TradeId = model.TradeId });

        // move trade items to the trade content history
        var moveTradeItemsResult = await _mediator.Send(new AddTradeItemsHistoryCommand { TradeId = model.TradeId, TradeItems = tradeItems });

        if (!moveTradeItemsResult.Success)
            return new BaseResult
            {
                Errors = moveTradeItemsResult.Errors
            };

        // clear the trade content
        var clarTradeContentResult = await _mediator.Send(new RemoveTradeItemsCommand { TradeId = model.TradeId, KeepCache = true });

        if (!clarTradeContentResult)
            return new BaseResult
            {
                Errors = new string[] { "Something went wrong" }
            };

        return new BaseResult
        {
            Success = true
        };
    }

    private Task ClearCacheUsedForTradeAsync(string tradeId, string senderId, string receiverId, string[] tradeItemIds)
    {
        var tasks = new Task[3 + tradeItemIds.Length];

        tasks[0] = _cacheService.ClearCacheKeyAsync(CacheKeys.Trade.GetTradeKey(tradeId));
        tasks[1] = _cacheService.ClearCacheKeyAsync(CacheKeys.Trade.GetSentTradeKey(senderId, tradeId));
        tasks[2] = _cacheService.ClearCacheKeyAsync(CacheKeys.Trade.GetReceivedTradeKey(receiverId, tradeId));
        for (int i = 0; i < tradeItemIds.Length; i++)
            tasks[3 + i] = _cacheService.RemoveFromSet(CacheKeys.UsedItem.GetUsedItemKey(tradeItemIds[i]), tradeId);

        return Task.WhenAll(tasks);
    }

    private async Task<string[]> GetSentTradeOffersIdListAsync(string userId, string[] tradeItems, bool responded = false)
    {
        var list = await _cacheService.GetEntityIdsAsync(
            CacheKeys.Trade.GetSentTradeKey(userId, ""),
            async (args) => await _context.SentTrades
                .AsNoTracking()
                .Where(st => Equals(st.SenderId, userId))
                .Select(t => t.TradeId)
                .ToArrayAsync(),
            true);

        return await FilterTradeOffersAsync(list, tradeItems, responded);
    }

    private async Task<string[]> GetReceivedTradeOffersIdListAsync(string userId, string[] tradeItems, bool responded = false)
    {
        var tradeOfferIds = await _cacheService.GetEntityIdsAsync(
            CacheKeys.Trade.GetReceivedTradeKey(userId, ""),
            async (args) => await _context.ReceivedTrades
                .AsNoTracking()
                .Where(o => Equals(userId, o.ReceiverId))
                .Select(t => t.TradeId)
                .ToArrayAsync(),
            true);

        return await FilterTradeOffersAsync(tradeOfferIds, tradeItems, responded);
    }

    private async Task<string[]> FilterTradeOffersAsync(string[] tradeOffersList, string[] tradeItems, bool responded = false)
    {
        var tradeIds = new List<string>();

        for (int i = 0; i < tradeOffersList.Length; i++)
        {
            string tradeOfferId = tradeOffersList[i];

            var response = await IsRespondedAsync(tradeOfferId);

            if (response == responded)
                tradeIds.Add(tradeOfferId);
        }

        var remainedTrades = new List<string>();

        if (tradeItems.Length > 0)
        {
            foreach (var tradeId in tradeIds)
            {
                bool keepTrade = false;

                for (int i = 0; i < tradeItems.Length; i++)
                {
                    bool hasTradeItem = await _mediator.Send(new HasTradeItemQuery { TradeId = tradeId, ItemId = tradeItems[i] });

                    if (hasTradeItem)
                    {
                        keepTrade = true;
                        break;
                    }
                }

                if (keepTrade) remainedTrades.Add(tradeId);
            }

            tradeIds = remainedTrades;
        }

        return tradeIds.ToArray();
    }

    private async Task ProcessTradeItemsFromInputModelAsync(CreateTradeOfferCommand model, List<Models.TradeItems.TradeItem> outputList)
    {
        var tasks = new List<Task>();

        foreach (var item in model.Items)
        {
            if (item is null || item.Price < 0)
                continue;

            // check if the user has the required quantity of this item
            if (!await _mediator.Send(_mapper.AdaptToType<Models.TradeItems.TradeItem, HasItemQuantityQuery>(item, ((string, object))(nameof(HasItemQuantityQuery.UserId), model.SenderUserId), (nameof(HasItemQuantityQuery.Notify), true))))
                continue;

            // lock the required quantity of this item
            if (!(await _mediator.Send(_mapper.AdaptToType<Models.TradeItems.TradeItem, LockItemCommand>(item, ((string, object))(nameof(LockItemCommand.UserId), model.SenderUserId), (nameof(LockItemCommand.Notify), true)))).Success)
                continue;

            var task = new Task(async () =>
            {
                var itemName = await GetItemNameAsync(item.ItemId);
                item.Name = itemName;
            });

            task.Start();

            tasks.Add(task);

            outputList.Add(item);
        }

        await Task.WhenAll(tasks);
    }

    private Task AddSentAndReceivedTradeEntitiesAsync(string tradeId, string senderUserId, string receiverUserId)
    {
        return Task.WhenAll(
                _context.AddAsync(
                    new Entities.SentTrade
                    {
                        TradeId = tradeId,
                        SenderId = senderUserId
                    }).AsTask(),
                _context.AddAsync(
                    new Entities.ReceivedTrade
                    {
                        TradeId = tradeId,
                        ReceiverId = receiverUserId
                    }).AsTask()
            );
    }

    private Task UpdateTradeEntityAsync(CachedTrade trade, bool response)
    {
        trade.Response = response;
        trade.ResponseDate = DateTime.Now;

        return _context.UpdateEntityAsync(
            new Entities.Trade
            {
                TradeId = trade.TradeId,
                Response = trade.Response,
                ResponseDate = trade.ResponseDate,
                SentDate = trade.SentDate
            });
    }

    private Task SetCacheForCreatedTradeAsync(Entities.Trade tradeEntity, CreateTradeOfferCommand model)
    {
        return Task.WhenAll(
            _cacheService.SetCacheValueAsync(CacheKeys.Trade.GetTradeKey(tradeEntity.TradeId), new CachedTrade
            {
                TradeId = tradeEntity.TradeId,
                SenderUserId = model.SenderUserId,
                ReceiverUserId = model.TargetUserId,
                TradeItemsId = model.Items.Select(x => x.ItemId).ToArray(),
                SentDate = tradeEntity.SentDate
            }),
            _cacheService.SetCacheValueAsync(CacheKeys.Trade.GetSentTradeKey(model.SenderUserId, tradeEntity.TradeId), ""),
            _cacheService.SetCacheValueAsync(CacheKeys.Trade.GetReceivedTradeKey(model.TargetUserId, tradeEntity.TradeId), "")
        );
    }

    private async Task<bool> IsRespondedAsync(string tradeId)
    {
        var tradeResponse = await GetTradeResponseAsync(tradeId);

        return tradeResponse is not null;
    }

    private async Task<string> GetReceiverIdAsync(string tradeId)
    {
        var cachedTrade = await GetCachedTradeAsync(tradeId);

        return cachedTrade?.ReceiverUserId ?? string.Empty;
    }

    private async Task<string> GetSenderIdAsync(string tradeId)
    {
        var cachedTrade = await GetCachedTradeAsync(tradeId);

        return cachedTrade?.SenderUserId ?? string.Empty;
    }

    private Task<CachedTrade> GetCachedTradeAsync(string tradeId)
    {
        return _cacheService.GetEntityAsync(
            CacheKeys.Trade.GetTradeKey(tradeId),
            async (args) =>
            {
                var trade = await GetTradeEntityAsync(tradeId);
                var sentTrade = await GetSentTradeEntityAsync(tradeId); ;
                var receivedTrade = await GetReceivedTradeEntityAsync(tradeId); ;

                var tradeItemIds = await Task.Run(async () =>
                {
                    var tradeItems = await GetTradeItemsAsync(trade.TradeId, trade.Response.HasValue /* if trade.Response has value, then it means it is a responded trade */ );

                    return tradeItems.Select(tradeItem => tradeItem.ItemId).ToArray();
                });

                return new CachedTrade
                {
                    TradeId = trade.TradeId,
                    SenderUserId = sentTrade.SenderId,
                    ReceiverUserId = receivedTrade.ReceiverId,
                    SentDate = trade.SentDate,
                    Response = trade.Response,
                    ResponseDate = trade.ResponseDate,
                    TradeItemsId = tradeItemIds
                };
            },
            true);
    }

    /// <returns>null -> trade has no response<br/> 
    /// true -> trade has 'Accepted' as response<br/>
    /// false -> trade has 'Declined' as response</returns>
    private async Task<bool?> GetTradeResponseAsync(string tradeId)
    {
        var cachedTrade = await GetCachedTradeAsync(tradeId);

        return cachedTrade.Response;
    }

    private async Task<DateTime?> GetTradeResponseDateAsync(string tradeId)
    {
        var cachedTrade = await GetCachedTradeAsync(tradeId);

        return cachedTrade?.ResponseDate;
    }

    private async Task<bool> UnlockTradeItemsAsync(string userId, string tradeId)
    {
        var tradeItems = await GetTradeItemsAsync(tradeId, false);
        var tasks = new Task[tradeItems.Length]; 

        for (int i = 0; i < tradeItems.Length; i++)
        {
            var item = tradeItems[i];

            if (item is null)
                continue;

            var request = _mapper.AdaptToType<Models.TradeItems.TradeItem, UnlockItemCommand>(item, ((string, object))(nameof(UnlockItemCommand.UserId), userId), (nameof(UnlockItemCommand.Notify), true));

            tasks[i] = _mediator.Send(request);
        }

        await Task.WhenAll(tasks);

        return tradeItems.Length != 0;
    }

    // Takes the items from trade to the receiver
    private async Task<bool> GiveItemsAsync(string userId, string tradeId)
    {
        var tradeItems = await GetTradeItemsAsync(tradeId, false);
        var tasks = new Task[tradeItems.Length];

        for (int i = 0; i < tradeItems.Length; i++)
        {
            var item = tradeItems[i];

            tasks[i] = _mediator.Send(_mapper.AdaptToType<Models.TradeItems.TradeItem, AddInventoryItemCommand>(item, ((string, object))(nameof(AddInventoryItemCommand.UserId), userId), (nameof(AddInventoryItemCommand.Notify), true)));
        }

        await Task.WhenAll(tasks);

        return tradeItems.Length != 0;
    }

    // Takes the items from the sender
    private async Task<bool> TakeItemsAsync(string userId, string tradeId)
    {
        var tradeItems = await GetTradeItemsAsync(tradeId, false);
        var tasks = new Task[tradeItems.Length];

        for (int i = 0; i < tradeItems.Length; i++)
        {
            var item = tradeItems[i];

            tasks[i] = _mediator.Send(_mapper.AdaptToType<Models.TradeItems.TradeItem, DropInventoryItemCommand>(item, ((string, object))(nameof(DropInventoryItemCommand.UserId), userId), (nameof(DropInventoryItemCommand.Notify), true)));
        }

        await Task.WhenAll(tasks);

        return tradeItems.Length != 0;
    }

    private Task<Entities.Trade> GetTradeEntityAsync(string tradeId) =>
        GetTradeQuery(_context, tradeId);

    private Task<Entities.SentTrade> GetSentTradeEntityAsync(string tradeId) =>
        GetSentTradeQuery(_context, tradeId);

    private Task<Entities.ReceivedTrade> GetReceivedTradeEntityAsync(string tradeId) =>
        GetReceivedTradeQuery(_context, tradeId);
    
    private async Task<int> GetTotalPriceAsync(string tradeOfferId)
    {
        var tradeItems = await GetTradeItemsAsync(tradeOfferId, false);

        if (tradeItems is null || tradeItems.Length == 0)
            return 0;

        int total = 0;

        foreach (var item in tradeItems)
            total += item.Price;

        return total;
    }

    private Task<string> GetItemNameAsync(string itemId) => _mediator.Send(new GetItemNameQuery { ItemId = itemId });

    private Task<string> GetUsernameAsync(string userId) => _mediator.Send(new GetUsernameQuery { UserId = userId });

    private Task<Models.TradeItems.TradeItem[]> GetTradeItemsAsync(string tradeId, bool responded) => responded ?
        _mediator.Send(new GetTradeItemsHistoryQuery { TradeId = tradeId }) :
        _mediator.Send(new GetTradeItemsQuery { TradeId = tradeId });

    #region Queries

    private static readonly Func<DatabaseContext, string, Task<Entities.Trade>> GetTradeQuery =
        EF.CompileAsyncQuery((DatabaseContext context, string tradeId) =>
            context.Trades
                .AsNoTracking()
                .FirstOrDefault(t => Equals(t.TradeId, tradeId))
        );
    
    private static readonly Func<DatabaseContext, string, Task<Entities.SentTrade>> GetSentTradeQuery =
        EF.CompileAsyncQuery((DatabaseContext context, string tradeId) =>
            context.SentTrades
                .AsNoTracking()
                .FirstOrDefault(t => Equals(t.TradeId, tradeId))
        );

    private static readonly Func<DatabaseContext, string, Task<Entities.ReceivedTrade>> GetReceivedTradeQuery =
        EF.CompileAsyncQuery((DatabaseContext context, string tradeId) =>
            context.ReceivedTrades
                .AsNoTracking()
                .FirstOrDefault(t => Equals(t.TradeId, tradeId))
        );

    #endregion Queries
}
