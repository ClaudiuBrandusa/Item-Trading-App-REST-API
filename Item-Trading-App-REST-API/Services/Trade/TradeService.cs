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
using Item_Trading_App_REST_API.Models.Inventory;
using Item_Trading_App_REST_API.Models.Item;
using Item_Trading_App_REST_API.Services.DatabaseContextWrapper;

namespace Item_Trading_App_REST_API.Services.Trade;

public class TradeService : ITradeService, IDisposable
{
    private readonly IDatabaseContextWrapper _databaseContextWrapper;
    private readonly DatabaseContext _context;
    private readonly ICacheService _cacheService;
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly IUnitOfWorkService _unitOfWork;

    public TradeService(IDatabaseContextWrapper databaseContextWrapper, ICacheService cacheService, IMediator mediator, IMapper mapper, IUnitOfWorkService unitOfWork)
    {
        _databaseContextWrapper = databaseContextWrapper;
        _context = databaseContextWrapper.ProvideDatabaseContext();
        _cacheService = cacheService;
        _mediator = mediator;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<TradeOfferResult> CreateTradeOfferAsync(CreateTradeOfferCommand model)
    {
        if (model is null || string.IsNullOrEmpty(model.SenderUserId) || string.IsNullOrEmpty(model.TargetUserId) || model.Items is null)
            return new TradeOfferResult
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
                return new TradeOfferResult
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
            return new TradeOfferResult
            {
                Errors = new[] { "Something went wrong" }
            };
        }

        var receiverUsernameTask = GetUsernameAsync(model.TargetUserId);
        var senderUsernameTask = GetUsernameAsync(model.SenderUserId);

        await Task.WhenAll(
            SetCacheForCreatedTradeAsync(offer, model),
            _mediator.Publish(new TradeCreatedEvent
            {
                TradeId = offer.TradeId,
                ReceiverId = model.TargetUserId,
            }),
            receiverUsernameTask,
            senderUsernameTask
        );

        return new TradeOfferResult
        {
            TradeId = offer.TradeId,
            SenderId = model.SenderUserId,
            SenderName = await senderUsernameTask,
            ReceiverId = model.TargetUserId,
            ReceiverName = await receiverUsernameTask,
            Items = items,
            CreationDate = offer.SentDate,
            Success = true
        };
    }

    public async Task<TradeOfferResult> AcceptTradeOfferAsync(RespondTradeCommand model)
    {
        if (string.IsNullOrEmpty(model.TradeId) || string.IsNullOrEmpty(model.UserId))
            return new TradeOfferResult
            {
                Errors = new[] { "Invalid IDs" }
            };

        string receiverId = await GetReceiverIdAsync(model.TradeId);

        if (!Equals(receiverId, model.UserId))
            return new TradeOfferResult
            {
                Errors = new[] { "Invalid userId" }
            };

        var trade = await GetCachedTradeAsync(model.TradeId);

        if (string.IsNullOrEmpty(trade.TradeId))
            return new TradeOfferResult
            {
                Errors = new[] { "Something went wrong" }
            };

        if (trade.Response is not null)
            return new TradeOfferResult
            {
                Errors = new[] { "Already responded" }
            };

        int price = await GetTotalPriceAsync(model.TradeId);

        if (price > await _mediator.Send(new GetUserCashQuery { UserId = model.UserId }))
            return new TradeOfferResult
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
                return new TradeOfferResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            senderId = await GetSenderIdAsync(model.TradeId);
            trade.SenderUserId = senderId;

            if (!await UnlockTradeItemsAsync(senderId, model.TradeId))
            {
                _unitOfWork.RollbackTransaction();
                return new TradeOfferResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            if (!await GiveItemsAsync(model.UserId, model.TradeId))
            {
                _unitOfWork.RollbackTransaction();
                return new TradeOfferResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            if (!await TakeItemsAsync(senderId, model.TradeId))
            {
                _unitOfWork.RollbackTransaction();
                return new TradeOfferResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            if (!await _mediator.Send(new GiveCashCommand { UserId = senderId, Amount = price }))
            {
                _unitOfWork.RollbackTransaction();
                return new TradeOfferResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            var respondTradeResult = await RespondTradeAsync(model);

            if (!respondTradeResult.Success)
            {
                _unitOfWork.RollbackTransaction();
                return new TradeOfferResult
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
            return new TradeOfferResult
            {
                Errors = new[] { "Something went wrong" }
            };
        }

        var senderNameTask = GetUsernameAsync(senderId);
        var receiverNameTask = GetUsernameAsync(receiverId);

        await Task.WhenAll(
            _cacheService.SetCacheValueAsync(CacheKeys.Trade.GetTradeKey(model.TradeId), trade),
            _mediator.Publish(new TradeRespondedEvent
            {
                TradeId = model.TradeId,
                SenderId = senderId,
                Response = true
            }),
            senderNameTask,
            receiverNameTask
        );

        return new TradeOfferResult
        {
            TradeId = model.TradeId,
            SenderId = senderId,
            SenderName = await senderNameTask,
            ReceiverId = receiverId,
            ReceiverName = await receiverNameTask,
            CreationDate = trade.SentDate,
            ResponseDate = trade.ResponseDate,
            Success = true
        };
    }

    public async Task<TradeOfferResult> RejectTradeOfferAsync(RespondTradeCommand model)
    {
        if (string.IsNullOrEmpty(model.TradeId) || string.IsNullOrEmpty(model.UserId))
            return new TradeOfferResult
            {
                Errors = new[] { "Invalid IDs" }
            };

        string receiverId = await GetReceiverIdAsync(model.TradeId);

        if (!Equals(receiverId, model.UserId))
            return new TradeOfferResult
            {
                Errors = new[] { "Invalid userId" }
            };

        var trade = await GetCachedTradeAsync(model.TradeId);

        if (string.IsNullOrEmpty(trade.TradeId))
            return new TradeOfferResult
            {
                Errors = new[] { "Something went wrong" }
            };

        if (trade.Response is not null)
            return new TradeOfferResult
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
                return new TradeOfferResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            var respondTradeResult = await RespondTradeAsync(model);

            if (!respondTradeResult.Success)
            {
                _unitOfWork.RollbackTransaction();
                return new TradeOfferResult
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
            return new TradeOfferResult
            {
                Errors = new[] { "Something went wrong" }
            };
        }

        var senderNameTask = GetUsernameAsync(senderId);
        var receiverNameTask = GetUsernameAsync(senderId);

        await Task.WhenAll(
            _cacheService.SetCacheValueAsync(CacheKeys.Trade.GetTradeKey(model.TradeId), trade),
            _mediator.Publish(new TradeRespondedEvent
            {
                TradeId = model.TradeId,
                SenderId = senderId,
                Response = false
            }),
            senderNameTask,
            receiverNameTask
        );

        return new TradeOfferResult
        {
            TradeId = model.TradeId,
            SenderId = senderId,
            SenderName = await senderNameTask,
            ReceiverId = receiverId,
            ReceiverName = await receiverNameTask,
            CreationDate = trade.SentDate,
            ResponseDate = trade.ResponseDate,
            Success = true
        };
    }

    public async Task<TradeOfferResult> CancelTradeOfferAsync(CancelTradeCommand model)
    {
        if (string.IsNullOrEmpty(model.TradeId) || string.IsNullOrEmpty(model.UserId))
            return new TradeOfferResult
            {
                Errors = new[] { "Invalid IDs" }
            };

        string senderId = await GetSenderIdAsync(model.TradeId);

        if (!Equals(senderId, model.UserId))
            return new TradeOfferResult
            {
                Errors = new[] { "Invalid userId" }
            };

        var trade = await GetCachedTradeAsync(model.TradeId);

        if (string.IsNullOrEmpty(trade.TradeId))
            return new TradeOfferResult
            {
                Errors = new[] { "Something went wrong" }
            };

        if (trade.Response.HasValue)
            return new TradeOfferResult
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
                return new TradeOfferResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            receiverId = await GetReceiverIdAsync(model.TradeId);
            trade.ReceiverUserId = receiverId;

            if (!await _context.RemoveEntityAsync(new Entities.Trade { TradeId = model.TradeId }))
            {
                _unitOfWork.RollbackTransaction();
                return new TradeOfferResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            await _context.SaveChangesAsync();

            _unitOfWork.CommitTransaction();
        }
        catch (Exception)
        {
            _unitOfWork.RollbackTransaction();
            return new TradeOfferResult
            {
                Errors = new[] { "Something went wrong" }
            };
        }

        var senderNameTask = GetUsernameAsync(senderId);
        var receiverNameTask = GetUsernameAsync(receiverId);

        await Task.WhenAll(
             ClearCacheUsedForTradeAsync(model.TradeId, senderId, receiverId, trade.TradeItemsId),
             _mediator.Publish(new TradeCancelledEvent
             {
                 TradeId = model.TradeId,
                 ReceiverId = receiverId
             }),
             senderNameTask,
             receiverNameTask
        );

        return new TradeOfferResult
        {
            TradeId = model.TradeId,
            SenderId = senderId,
            SenderName = await senderNameTask,
            ReceiverId = receiverId,
            ReceiverName = await receiverNameTask,
            CreationDate = trade.SentDate,
            Success = true
        };
    }

    public async Task<TradeOffersResult> GetTradeOffersAsync(ListTradesQuery model)
    {
        if (string.IsNullOrEmpty(model.UserId))
            return new TradeOffersResult
            {
                Errors = new[] { "Invalid input data" }
            };

        var sentTradeOfferIds = Array.Empty<string>();
        var receivedTradeOfferIds = Array.Empty<string>();

        if (model.TradeDirection == TradeDirection.All || model.TradeDirection == TradeDirection.Sent)
            sentTradeOfferIds = await GetSentTradeOffersIdListAsync(model.UserId, model.TradeItemIds, model.Responded);

        if (model.TradeDirection == TradeDirection.All || model.TradeDirection == TradeDirection.Received)
            receivedTradeOfferIds = await GetReceivedTradeOffersIdListAsync(model.UserId, model.TradeItemIds, model.Responded);

        if (sentTradeOfferIds is null || receivedTradeOfferIds is null)
            return new TradeOffersResult
            {
                Errors = new[] { "Something went wrong" }
            };

        return new TradeOffersResult
        {
            SentTradeOfferIds = sentTradeOfferIds,
            ReceivedTradeOfferIds = receivedTradeOfferIds,
            Success = true
        };
    }

    public async Task<TradeOfferResult> GetTradeOfferAsync(RequestTradeOfferQuery requestTradeOffer)
    {
        if (requestTradeOffer is null || string.IsNullOrEmpty(requestTradeOffer.TradeId))
            return new TradeOfferResult
            {
                Errors = new[] { "Invalid input data" }
            };

        var trade = await GetCachedTradeAsync(requestTradeOffer.TradeId);

        var senderNameTask = GetUsernameAsync(trade.SenderUserId);
        var receiverNameTask = GetUsernameAsync(trade.ReceiverUserId);
        var tradeItemsTask = GetTradeItemsAsync(requestTradeOffer.TradeId, false);

        await Task.WhenAll(
            senderNameTask,
            receiverNameTask,
            tradeItemsTask
        );

        return new TradeOfferResult
        {
            TradeId = trade.TradeId,
            SenderId = trade.SenderUserId,
            SenderName = await senderNameTask,
            ReceiverId = trade.ReceiverUserId,
            ReceiverName = await receiverNameTask,
            CreationDate = trade.SentDate,
            Response = trade.Response,
            ResponseDate = trade.ResponseDate,
            Items = await tradeItemsTask,
            Success = true
        };
    }

    public void Dispose()
    {
        _databaseContextWrapper.Dispose(_context);
        GC.SuppressFinalize(this);
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

        var remainedTradeIds = new List<string>();

        if (tradeItems.Length > 0)
        {
            await FilterTradesByTradeItemsAsync(tradeIds, tradeItems, remainedTradeIds);

            tradeIds = remainedTradeIds;
        }

        return tradeIds.ToArray();
    }

    private async Task FilterTradesByTradeItemsAsync(List<string> tradeIds, string[] tradeItems, List<string> remainedTradeIds)
    {
        foreach (var tradeId in tradeIds)
        {
            bool keepTrade = false;

            for (int i = 0; i < tradeItems.Length; i++)
            {
                bool hasTradeItem = await _mediator.Send(
                    new HasTradeItemQuery
                    {
                        TradeId = tradeId,
                        ItemId = tradeItems[i]
                    }
                );

                if (hasTradeItem)
                {
                    keepTrade = true;
                    break;
                }
            }

            if (keepTrade) remainedTradeIds.Add(tradeId);
        }
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

    private async ValueTask AddSentAndReceivedTradeEntitiesAsync(string tradeId, string senderUserId, string receiverUserId)
    {
        var sentTradeTask = _context.AddAsync(
            new Entities.SentTrade
            {
                TradeId = tradeId,
                SenderId = senderUserId
            });
        var receivedTradeTask = _context.AddAsync(
            new Entities.ReceivedTrade
            {
                TradeId = tradeId,
                ReceiverId = receiverUserId
            });

        await sentTradeTask;
        await receivedTradeTask;
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

        return cachedTrade.ReceiverUserId ?? string.Empty;
    }

    private async Task<string> GetSenderIdAsync(string tradeId)
    {
        var cachedTrade = await GetCachedTradeAsync(tradeId);

        return cachedTrade.SenderUserId ?? string.Empty;
    }

    private Task<CachedTrade> GetCachedTradeAsync(string tradeId)
    {
        return _cacheService.GetEntityAsync(
            CacheKeys.Trade.GetTradeKey(tradeId),
            async (args) =>
            {
                var tradeTask = GetTradeEntityAsync(tradeId);
                var sentTradeTask = GetSentTradeEntityAsync(tradeId);
                var receivedTradeTask = GetReceivedTradeEntityAsync(tradeId);

                var trade = await tradeTask;

                var tradeItems = await GetTradeItemsAsync(trade.TradeId, trade.Response.HasValue /* if trade.Response has value, then it means it is a responded trade */ );

                var tradeItemIds = tradeItems.Select(tradeItem => tradeItem.ItemId).ToArray();

                return new CachedTrade
                {
                    TradeId = trade.TradeId,
                    SenderUserId = (await sentTradeTask).SenderId,
                    ReceiverUserId = (await receivedTradeTask).ReceiverId,
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

    private async Task<bool> UnlockTradeItemsAsync(string userId, string tradeId)
    {
        var tradeItems = await GetTradeItemsAsync(tradeId, false);

        var tasks = new Task<LockItemResult>[tradeItems.Length];

        for (int i = 0; i < tradeItems.Length; i++)
        {
            var item = tradeItems[i];

            if (item is null)
                continue;

            var request = _mapper.AdaptToType<Models.TradeItems.TradeItem, UnlockItemCommand>(item, ((string, object))(nameof(UnlockItemCommand.UserId), userId), (nameof(UnlockItemCommand.Notify), true));

            tasks[i] = _mediator.Send(request);
        }

        await Task.WhenAll(tasks);

        if (tasks.Select(x => x.Result).Any(x => !x.Success))
            return false;

        return tradeItems.Length != 0;
    }

    // Takes the items from trade to the receiver
    private async Task<bool> GiveItemsAsync(string userId, string tradeId)
    {
        var tradeItems = await GetTradeItemsAsync(tradeId, false);
        var tasks = new Task<QuantifiedItemResult>[tradeItems.Length];

        for (int i = 0; i < tradeItems.Length; i++)
        {
            var item = tradeItems[i];

            tasks[i] = _mediator.Send(_mapper.AdaptToType<Models.TradeItems.TradeItem, AddInventoryItemCommand>(item, ((string, object))(nameof(AddInventoryItemCommand.UserId), userId), (nameof(AddInventoryItemCommand.Notify), true)));
        }

        await Task.WhenAll(tasks);

        if (tasks.Select(x => x.Result).Any(x => !x.Success))
            return false;

        return tradeItems.Length != 0;
    }

    // Takes the items from the sender
    private async Task<bool> TakeItemsAsync(string userId, string tradeId)
    {
        var tradeItems = await GetTradeItemsAsync(tradeId, false);
        var tasks = new Task<QuantifiedItemResult>[tradeItems.Length];

        for (int i = 0; i < tradeItems.Length; i++)
        {
            var item = tradeItems[i];

            tasks[i] = _mediator.Send(_mapper.AdaptToType<Models.TradeItems.TradeItem, DropInventoryItemCommand>(item, ((string, object))(nameof(DropInventoryItemCommand.UserId), userId), (nameof(DropInventoryItemCommand.Notify), true)));
        }

        await Task.WhenAll(tasks);

        if (tasks.Select(x => x.Result).Any(x => !x.Success))
            return false;

        return tradeItems.Length != 0;
    }

    private async Task<Entities.Trade> GetTradeEntityAsync(string tradeId)
    {
        var dbContext = await _databaseContextWrapper.ProvideDatabaseContextAsync();

        var tradeEntity = await GetTradeQuery(dbContext, tradeId);

        _databaseContextWrapper.Dispose(dbContext);

        return tradeEntity;
    }

    private async Task<Entities.SentTrade> GetSentTradeEntityAsync(string tradeId)
    {
        var dbContext = await _databaseContextWrapper.ProvideDatabaseContextAsync();

        var sentTradeEntity = await GetSentTradeQuery(dbContext, tradeId);

        _databaseContextWrapper.Dispose(dbContext);

        return sentTradeEntity;
    }

    private async Task<Entities.ReceivedTrade> GetReceivedTradeEntityAsync(string tradeId)
    {
        var dbContext = await _databaseContextWrapper.ProvideDatabaseContextAsync();

        var receivedTradeEntity = await GetReceivedTradeQuery(dbContext, tradeId);

        _databaseContextWrapper.Dispose(dbContext);

        return receivedTradeEntity;
    }
    
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
