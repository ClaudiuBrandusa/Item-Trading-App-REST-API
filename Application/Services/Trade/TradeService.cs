using MediatR;
using MapsterMapper;
using Application.Behaviors.Inventory.AddItem;
using Application.Models.Inventory;
using Application.Behaviors.Inventory.DropItem;
using Application.Behaviors.Inventory.HasItem;
using Application.Behaviors.Inventory.UnlockItem;
using Application.Behaviors.Trade.CreateTrade;
using Application.Models.Trade;
using Application.Behaviors.Wallet.GetCash;
using Application.Behaviors.Wallet.TakeCash;
using Application.Behaviors.Wallet.GiveCash;
using Application.Constants;
using Application.Models.Base;
using Application.Behaviors.Trade.RespondTrade;
using Application.Behaviors.Trade.ListTrades;
using Application.Extensions;
using Application.Behaviors.Trade.CancelTrade;
using Application.Behaviors.Trade.GetTrade;
using Application.Behaviors.TradeItemHistory.AddTradeItems;
using Application.Behaviors.Identity.GetUsername;
using Application.Behaviors.Item.GetItemName;
using Application.Behaviors.TradeItem.RemoveTradeItems;
using Application.Behaviors.TradeItem.GetTradeItems;
using Application.Behaviors.TradeItem.HasTradeItem;
using Application.Behaviors.TradeItem.AddTradeItem;
using Application.Behaviors.Inventory.LockItem;
using Application.Services.UnitOfWork;
using Application.Services.Cache;
using Domain.Repositories;
using Domain.Trade;

namespace Application.Services.Trade;

public class TradeService : ITradeService, IDisposable
{
    private readonly ITradeRepository _repository;
    private readonly ICacheService _cacheService;
    private readonly ISender _sender;
    private readonly IPublisher _publisher;
    private readonly IMapper _mapper;
    private readonly IUnitOfWorkService _unitOfWork;

    public TradeService(ITradeRepository tradeRepository, ICacheService cacheService, ISender sender, IPublisher publisher, IMapper mapper, IUnitOfWorkService unitOfWork)
    {
        _repository = tradeRepository;
        _cacheService = cacheService;
        _sender = sender;
        _publisher = publisher;
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

        var items = new List<Domain.TradeItems.TradeItem>();
        Domain.Trades.Trade offer;

        _unitOfWork.BeginTransaction();

        try
        {
            await ProcessTradeItemsFromInputModelAsync(model, items);

            if (items.Count == 0)
                return new TradeOfferResult
                {
                    Errors = new[] { "Invalid input data" }
                };

            offer = new Domain.Trades.Trade
            {
                TradeId = Guid.NewGuid().ToString(),
                SentDate = DateTime.Now
            };

            await _repository.AddEntityAsync(offer);

            foreach (var item in items)
            {
                var request = _mapper.AdaptToType<Domain.TradeItems.TradeItem, AddTradeItemCommand>(item, ((string, object))(nameof(AddTradeItemCommand.TradeId), offer.TradeId));
                if (!await _sender.Send(request))
                {
                    _unitOfWork.RollbackTransaction();
                    break;
                }
            }

            await _repository.AddSentAndReceivedTradeEntitiesAsync(offer.TradeId, model.SenderUserId, model.TargetUserId);

            await _repository.SaveChangesAsync();

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
            _publisher.Publish(new TradeCreatedEvent
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

        var trade = await _repository.GetCachedTradeAsync(model.TradeId);

        if (trade == null || string.IsNullOrEmpty(trade.TradeId) || string.IsNullOrEmpty(trade.SenderUserId) || string.IsNullOrEmpty(trade.ReceiverUserId))
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

        if (price > await _sender.Send(new GetUserCashQuery { UserId = model.UserId }))
            return new TradeOfferResult
            {
                Errors = new[] { "User has not enough money" }
            };

        _unitOfWork.BeginTransaction();

        string senderId;

        try
        {
            if (!await _sender.Send(new TakeCashCommand { UserId = model.UserId, Amount = price }))
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

            if (!await _sender.Send(new GiveCashCommand { UserId = senderId, Amount = price }))
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
            _publisher.Publish(new TradeRespondedEvent
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

        var trade = await _repository.GetCachedTradeAsync(model.TradeId);

        if (trade == null || string.IsNullOrEmpty(trade.TradeId) || string.IsNullOrEmpty(trade.SenderUserId) || string.IsNullOrEmpty(trade.ReceiverUserId))
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
            _publisher.Publish(new TradeRespondedEvent
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

        var trade = await _repository.GetCachedTradeAsync(model.TradeId);

        if (trade == null || string.IsNullOrEmpty(trade.TradeId) || string.IsNullOrEmpty(trade.SenderUserId) || string.IsNullOrEmpty(trade.ReceiverUserId))
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

            if (!await _repository.RemoveEntityAsync(new Domain.Trades.Trade { TradeId = model.TradeId }))
            {
                _unitOfWork.RollbackTransaction();
                return new TradeOfferResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            await _repository.SaveChangesAsync();

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
             _publisher.Publish(new TradeCancelledEvent
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

        var trade = await _repository.GetCachedTradeAsync(requestTradeOffer.TradeId);

        if (trade == null || string.IsNullOrEmpty(trade.TradeId) || string.IsNullOrEmpty(trade.SenderUserId) || string.IsNullOrEmpty(trade.ReceiverUserId))
            return new TradeOfferResult
            {
                Errors = new[] { "Something went wrong" }
            };

        var senderNameTask = GetUsernameAsync(trade.SenderUserId);
        var receiverNameTask = GetUsernameAsync(trade.ReceiverUserId);
        var tradeItemsTask = _repository.GetTradeItemsAsync(requestTradeOffer.TradeId, false);

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
        _repository.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task<BaseResult> RespondTradeAsync(RespondTradeCommand model)
    {
        // get trade items
        var tradeItems = await _sender.Send(new GetTradeItemsQuery { TradeId = model.TradeId });

        // move trade items to the trade content history
        var moveTradeItemsResult = await _sender.Send(new AddTradeItemsHistoryCommand { TradeId = model.TradeId, TradeItems = tradeItems });

        if (!moveTradeItemsResult.Success)
            return new BaseResult
            {
                Errors = moveTradeItemsResult.Errors
            };

        // clear the trade content
        var clarTradeContentResult = await _sender.Send(new RemoveTradeItemsCommand { TradeId = model.TradeId, KeepCache = true });

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
        var list = await _repository.ListSentTradeIdsCachedAsync(userId);

        return await FilterTradeOffersAsync(list, tradeItems, responded);
    }

    private async Task<string[]> GetReceivedTradeOffersIdListAsync(string userId, string[] tradeItems, bool responded = false)
    {
        var tradeOfferIds = await _repository.ListReceivedTradeIdsCachedAsync(userId);

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
                bool hasTradeItem = await _sender.Send(
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

    private async Task ProcessTradeItemsFromInputModelAsync(CreateTradeOfferCommand model, List<Domain.TradeItems.TradeItem> outputList)
    {
        var tasks = new List<Task>();

        foreach (var item in model.Items)
        {
            if (item is null || item.Price < 0)
                continue;

            // check if the user has the required quantity of this item
            if (!await _sender.Send(_mapper.AdaptToType<Domain.TradeItems.TradeItem, HasItemQuantityQuery>(item, ((string, object))(nameof(HasItemQuantityQuery.UserId), model.SenderUserId), (nameof(HasItemQuantityQuery.Notify), true))))
                continue;

            // lock the required quantity of this item
            if (!(await _sender.Send(_mapper.AdaptToType<Domain.TradeItems.TradeItem, LockItemCommand>(item, ((string, object))(nameof(LockItemCommand.UserId), model.SenderUserId), (nameof(LockItemCommand.Notify), true)))).Success)
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

    private Task UpdateTradeEntityAsync(CachedTrade trade, bool response)
    {
        trade.Response = response;
        trade.ResponseDate = DateTime.Now;

        return _repository.UpdateEntityAsync(
            new Domain.Trades.Trade
            {
                TradeId = trade.TradeId,
                Response = trade.Response,
                ResponseDate = trade.ResponseDate,
                SentDate = trade.SentDate
            });
    }

    private Task SetCacheForCreatedTradeAsync(Domain.Trades.Trade tradeEntity, CreateTradeOfferCommand model)
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
        var cachedTrade = await _repository.GetCachedTradeAsync(tradeId);

        return cachedTrade?.ReceiverUserId ?? string.Empty;
    }

    private async Task<string> GetSenderIdAsync(string tradeId)
    {
        var cachedTrade = await _repository.GetCachedTradeAsync(tradeId);

        return cachedTrade?.SenderUserId ?? string.Empty;
    }

    /// <returns>null -> trade has no response<br/> 
    /// true -> trade has 'Accepted' as response<br/>
    /// false -> trade has 'Declined' as response</returns>
    private async Task<bool?> GetTradeResponseAsync(string tradeId)
    {
        var cachedTrade = await _repository.GetCachedTradeAsync(tradeId);

        return cachedTrade?.Response;
    }

    private async Task<bool> UnlockTradeItemsAsync(string userId, string tradeId)
    {
        var tradeItems = await _repository.GetTradeItemsAsync(tradeId, false);

        var tasks = new Task<LockItemResult>[tradeItems.Length];

        for (int i = 0; i < tradeItems.Length; i++)
        {
            var item = tradeItems[i];

            if (item is null)
                continue;

            var request = _mapper.AdaptToType<Domain.TradeItems.TradeItem, UnlockItemCommand>(item, ((string, object))(nameof(UnlockItemCommand.UserId), userId), (nameof(UnlockItemCommand.Notify), true));

            tasks[i] = _sender.Send(request);
        }

        await Task.WhenAll(tasks);

        if (tasks.Select(x => x.Result).Any(x => !x.Success))
            return false;

        return tradeItems.Length != 0;
    }

    // Takes the items from trade to the receiver
    private async Task<bool> GiveItemsAsync(string userId, string tradeId)
    {
        var tradeItems = await _repository.GetTradeItemsAsync(tradeId, false);
        var tasks = new Task<QuantifiedItemResult>[tradeItems.Length];

        for (int i = 0; i < tradeItems.Length; i++)
        {
            var item = tradeItems[i];

            tasks[i] = _sender.Send(_mapper.AdaptToType<Domain.TradeItems.TradeItem, AddInventoryItemCommand>(item, ((string, object))(nameof(AddInventoryItemCommand.UserId), userId), (nameof(AddInventoryItemCommand.Notify), true)));
        }

        await Task.WhenAll(tasks);

        if (tasks.Select(x => x.Result).Any(x => !x.Success))
            return false;

        return tradeItems.Length != 0;
    }

    // Takes the items from the sender
    private async Task<bool> TakeItemsAsync(string userId, string tradeId)
    {
        var tradeItems = await _repository.GetTradeItemsAsync(tradeId, false);
        var tasks = new Task<QuantifiedItemResult>[tradeItems.Length];

        for (int i = 0; i < tradeItems.Length; i++)
        {
            var item = tradeItems[i];

            tasks[i] = _sender.Send(_mapper.AdaptToType<Domain.TradeItems.TradeItem, DropInventoryItemCommand>(item, ((string, object))(nameof(DropInventoryItemCommand.UserId), userId), (nameof(DropInventoryItemCommand.Notify), true)));
        }

        await Task.WhenAll(tasks);

        if (tasks.Select(x => x.Result).Any(x => !x.Success))
            return false;

        return tradeItems.Length != 0;
    }

    private async Task<int> GetTotalPriceAsync(string tradeOfferId)
    {
        var tradeItems = await _repository.GetTradeItemsAsync(tradeOfferId, false);

        if (tradeItems is null || tradeItems.Length == 0)
            return 0;

        int total = 0;

        foreach (var item in tradeItems)
            total += item.Price;

        return total;
    }

    private Task<string> GetItemNameAsync(string itemId) => _sender.Send(new GetItemNameQuery { ItemId = itemId });

    private Task<string> GetUsernameAsync(string userId) => _sender.Send(new GetUsernameQuery { UserId = userId });
}
