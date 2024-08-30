using MediatR;
using MapsterMapper;
using Application.Behaviors.Inventory.AddItem;
using Application.Behaviors.Inventory.DropItem;
using Application.Behaviors.Inventory.HasItem;
using Application.Behaviors.Inventory.UnlockItem;
using Application.Behaviors.Trade.CreateTrade;
using Application.Behaviors.Wallet.GetCash;
using Application.Behaviors.Wallet.TakeCash;
using Application.Behaviors.Wallet.GiveCash;
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
using Application.Repositories;
using Application.Models;
using Application.Results.Trades;
using Application.Models.Trades;
using Domain.Entities.Trades;
using Application.Models.TradeItems;

namespace Application.Services.Trade;

public class TradeService : ITradeService, IDisposable
{
    private readonly ICachedTradeRepository _repository;
    private readonly ISender _sender;
    private readonly IPublisher _publisher;
    private readonly IMapper _mapper;
    private readonly IUnitOfWorkService _unitOfWork;

    public TradeService(ICachedTradeRepository tradeRepository, ISender sender, IPublisher publisher, IMapper mapper, IUnitOfWorkService unitOfWork)
    {
        _repository = tradeRepository;
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

        var items = new List<TradeItemDTO>();
        Domain.Aggregates.Trades.Trade offer = null;

        var transactionError = await _unitOfWork.ExplicitTransaction(async (TaskCompletionSource<TradeOfferResult?> taskCompletionSource) =>
        {
            try
            {
                var processTradeItemsResult = await ProcessTradeItemsFromInputModelAsync(model, items);

                if (!processTradeItemsResult.Success)
                {
                    taskCompletionSource.SetResult(new TradeOfferResult
                    {
                        Errors = processTradeItemsResult.Errors
                    });

                    return false;
                }

                if (items.Count == 0)
                {
                    taskCompletionSource.SetResult(new TradeOfferResult
                    {
                        Errors = new[] { "Invalid input data" }
                    });

                    return false;
                }

                offer = new Domain.Aggregates.Trades.Trade(DateTime.Now);

                await _repository.AddEntityAsync(offer);

                foreach (var item in items)
                {
                    var request = _mapper.AdaptToType<TradeItemDTO, AddTradeItemCommand>(item, (nameof(AddTradeItemCommand.TradeId), offer.TradeId));
                    if (!await _sender.Send(request))
                    {
                        taskCompletionSource.SetResult(new TradeOfferResult
                        {
                            Errors = new[] { "Something went wrong" }
                        });

                        return false;
                    }
                }

                await _repository.AddSentAndReceivedTradeEntitiesAsync(offer.TradeId, model.SenderUserId, model.TargetUserId);

                await _repository.SaveChangesAsync();

                return true;
            }
            catch (Exception)
            {
                taskCompletionSource.SetResult(new TradeOfferResult
                {
                    Errors = new[] { "Something went wrong" }
                });

                return false;
            }
        });

        if (transactionError is not null)
            return transactionError;

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

        string senderId = string.Empty;

        var transactionError = await _unitOfWork.ExplicitTransaction(async (TaskCompletionSource<TradeOfferResult?> taskCompletionSource) =>
        {
            try
            {
                if (!await _sender.Send(new TakeCashCommand { UserId = model.UserId, Amount = price }))
                {
                    taskCompletionSource.SetResult(new TradeOfferResult
                    {
                        Errors = new[] { "Something went wrong" }
                    });

                    return false;
                }

                senderId = await GetSenderIdAsync(model.TradeId);
                trade.SenderUserId = senderId;

                var unlockTradeItemsResult = await UnlockTradeItemsAsync(senderId, model.TradeId);

                if (!unlockTradeItemsResult.Success)
                {
                    taskCompletionSource.SetResult(new TradeOfferResult
                    {
                        Errors = new[] { "Something went wrong" }
                    });

                    return false;
                }

                var giveItemsResult = await GiveItemsAsync(model.UserId, model.TradeId);

                if (!giveItemsResult.Success)
                {
                    taskCompletionSource.SetResult(new TradeOfferResult
                    {
                        Errors = new[] { "Something went wrong" }
                    });

                    return false;
                }

                var takeItemsResult = await TakeItemsAsync(senderId, model.TradeId);

                if (!takeItemsResult.Success)
                {
                    taskCompletionSource.SetResult(new TradeOfferResult
                    {
                        Errors = new[] { "Something went wrong" }
                    });

                    return false;
                }

                if (!await _sender.Send(new GiveCashCommand { UserId = senderId, Amount = price }))
                {
                    taskCompletionSource.SetResult(new TradeOfferResult
                    {
                        Errors = new[] { "Something went wrong" }
                    });

                    return false;
                }

                var respondTradeResult = await RespondTradeAsync(model);

                if (!respondTradeResult.Success)
                {
                    taskCompletionSource.SetResult(new TradeOfferResult
                    {
                        Errors = respondTradeResult.Errors
                    });

                    return false;
                }

                return await UpdateTradeEntityAsync(trade, true);
            }
            catch (Exception)
            {
                taskCompletionSource.SetResult(new TradeOfferResult
                {
                    Errors = new[] { "Something went wrong" }
                });

                return false;
            }
        });

        if (transactionError is not null)
            return transactionError;

        var senderNameTask = GetUsernameAsync(senderId);
        var receiverNameTask = GetUsernameAsync(receiverId);

        await Task.WhenAll(
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
            Response = trade.Response,
            Items = trade.TradeItems,
            Success = true
        };
    }

    public async Task<TradeOfferResult> RejectTradeOfferAsync(RespondTradeCommand model)
    {
        if (string.IsNullOrEmpty(model.TradeId) || string.IsNullOrEmpty(model.UserId))
            new TradeOfferResult
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

        var transactionError = await _unitOfWork.ExplicitTransaction(async (TaskCompletionSource<TradeOfferResult?> taskCompletionSource) =>
        {
            try
            {
                var unlockTradeItemsResult = await UnlockTradeItemsAsync(senderId, model.TradeId);

                if (!unlockTradeItemsResult.Success)
                {
                    taskCompletionSource.SetResult(new TradeOfferResult
                    {
                        Errors = new[] { "Something went wrong" }
                    });
                    return false;
                }

                var respondTradeResult = await RespondTradeAsync(model);

                if (!respondTradeResult.Success)
                {
                    taskCompletionSource.SetResult(new TradeOfferResult
                    {
                        Errors = respondTradeResult.Errors
                    });
                    return false;
                }

                return await UpdateTradeEntityAsync(trade, false);
            }
            catch (Exception)
            {
                taskCompletionSource.SetResult(new TradeOfferResult
                {
                    Errors = new[] { "Something went wrong" }
                });
                return false;
            }
        });

        if (transactionError is not null)
            return transactionError;

        var senderNameTask = GetUsernameAsync(senderId);
        var receiverNameTask = GetUsernameAsync(senderId);

        await Task.WhenAll(
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
            Response = trade.Response,
            Items = trade.TradeItems,
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

        string receiverId = string.Empty;

        var transactionError = await _unitOfWork.ExplicitTransaction(async (TaskCompletionSource<TradeOfferResult?> taskCompletionSource) =>
        {
            try
            {
                var unlockTradeItemsResult = await UnlockTradeItemsAsync(model.UserId, model.TradeId);

                if (!unlockTradeItemsResult.Success)
                {
                    taskCompletionSource.SetResult(new TradeOfferResult
                    {
                        Errors = new[] { "Something went wrong" }
                    });
                    return false;
                }

                receiverId = await GetReceiverIdAsync(model.TradeId);
                trade.ReceiverUserId = receiverId;

                if (!await _repository.RemoveEntityAsync(new Domain.Aggregates.Trades.Trade(model.TradeId, DateTime.Now)))
                {
                    taskCompletionSource.SetResult(new TradeOfferResult
                    {
                        Errors = new[] { "Something went wrong" }
                    });
                    return false;
                }

                return await _repository.SaveChangesAsync() > 0;
            }
            catch (Exception)
            {
                taskCompletionSource.SetResult(new TradeOfferResult
                {
                    Errors = new[] { "Something went wrong" }
                });
                return false;
            }
        });

        if (transactionError is not null)
            return transactionError;

        var senderNameTask = GetUsernameAsync(senderId);
        var receiverNameTask = GetUsernameAsync(receiverId);

        await Task.WhenAll(
             ClearCacheUsedForTradeAsync(model.TradeId, senderId, receiverId, trade.TradeItems.Select(x => x.ItemId).ToArray()),
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
            ResponseDate = trade.ResponseDate,
            Response = trade.Response,
            Items = trade.TradeItems,
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
        var tradeItemsTask = _repository.GetTradeItemsAsync(trade.TradeId, trade.Response is not null);

        await Task.WhenAll(
            senderNameTask,
            receiverNameTask,
            tradeItemsTask
        );

        var tradeItems = await tradeItemsTask;
        var tradeItemsData = new TradeItemDTO[tradeItems.Length];

        for (int i = 0; i < tradeItems.Length; i++)
        {
            string itemName = await GetItemNameAsync(tradeItems[i].ItemId);

            tradeItemsData[i] = _mapper.AdaptToType<TradeItem, TradeItemDTO>(tradeItems[i], (nameof(TradeItemDTO.ItemName), itemName));
        }


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
            Items = tradeItemsData,
            Success = true
        };
    }

    public void Dispose()
    {
        _repository.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task<Result> RespondTradeAsync(RespondTradeCommand model)
    {
        // get trade items
        var tradeItems = await _sender.Send(new GetTradeItemsQuery { TradeId = model.TradeId });

        // move trade items to the trade content history
        var moveTradeItemsResult = await _sender.Send(new AddTradeItemsHistoryCommand { TradeId = model.TradeId, TradeItems = tradeItems });

        if (!moveTradeItemsResult.Success)
            return new Result
            {
                Errors = moveTradeItemsResult.Errors
            };

        // clear the trade content
        var clarTradeContentResult = await _sender.Send(new RemoveTradeItemsCommand { TradeId = model.TradeId, KeepCache = true });

        if (!clarTradeContentResult)
            return new Result
            {
                Errors = new string[] { "Something went wrong" }
            };

        return new Result
        {
            Success = true
        };
    }

    private Task ClearCacheUsedForTradeAsync(string tradeId, string senderId, string receiverId, string[] tradeItemIds) =>
        _repository.ClearTradeCache(tradeId, senderId, receiverId, tradeItemIds);

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

    private async Task<Result> ProcessTradeItemsFromInputModelAsync(CreateTradeOfferCommand model, List<TradeItemDTO> outputList)
    {
        var tasks = new List<Task>();

        foreach (var item in model.Items)
        {
            if (item is null || item.Price < 0)
                continue;

            // check if the user has the required quantity of this item
            if (!await _sender.Send(_mapper.AdaptToType<TradeItemDTO, HasItemQuantityQuery>(item, ((string, object))(nameof(HasItemQuantityQuery.UserId), model.SenderUserId), (nameof(HasItemQuantityQuery.Notify), true))))
                return new Result
                {
                    Errors = new string[]
                    {
                        $"User does not own the quantity {item.Quantity} of item with id {item.ItemId}"
                    }
                };

            // lock the required quantity of this item
            if (!(await _sender.Send(_mapper.AdaptToType<TradeItemDTO, LockItemCommand>(item, ((string, object))(nameof(LockItemCommand.UserId), model.SenderUserId), (nameof(LockItemCommand.Notify), true)))).Success)
                continue;

            var task = new Task(async () =>
            {
                var itemName = await GetItemNameAsync(item.ItemId);
                item.ItemName = itemName;
            });

            task.Start();

            tasks.Add(task);

            outputList.Add(item);
        }

        await Task.WhenAll(tasks);

        return new Result
        {
            Success = true
        };
    }

    private Task<bool> UpdateTradeEntityAsync(CachedTrade trade, bool response)
    {
        trade.Response = response;
        trade.ResponseDate = DateTime.Now;

        return _repository.UpdateEntityAsync(
            new Domain.Aggregates.Trades.Trade(trade.TradeId, trade.SentDate, trade.ResponseDate, trade.Response));
    }

    private Task SetCacheForCreatedTradeAsync(Domain.Aggregates.Trades.Trade tradeEntity, CreateTradeOfferCommand model)
    {
        return _repository.SetCacheForTrade(tradeEntity, model.SenderUserId, model.TargetUserId, model.Items.ToArray());
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

    private async Task<Result> UnlockTradeItemsAsync(string userId, string tradeId)
    {
        var tradeItems = await _repository.GetTradeItemsAsync(tradeId, false);

        for (int i = 0; i < tradeItems.Length; i++)
        {
            var item = tradeItems[i];

            if (item is null)
                continue;

            var request = _mapper.AdaptToType<TradeItem, UnlockItemCommand>(item, ((string, object))(nameof(UnlockItemCommand.UserId), userId), (nameof(UnlockItemCommand.Notify), true));

            var result = await _sender.Send(request);

            if (!result.Success)
            {
                return new Result
                {
                    Errors = result.Errors
                };
            }
        }

        return new Result
        {
            Success = true
        };
    }

    // Takes the items from trade to the receiver
    private async Task<Result> GiveItemsAsync(string userId, string tradeId)
    {
        var tradeItems = await _repository.GetTradeItemsAsync(tradeId, false);
        
        for (int i = 0; i < tradeItems.Length; i++)
        {
            var item = tradeItems[i];

            var result = await _sender.Send(_mapper.AdaptToType<TradeItem, AddInventoryItemCommand>(item, ((string, object))(nameof(AddInventoryItemCommand.UserId), userId), (nameof(AddInventoryItemCommand.Notify), true)));
            
            if (!result.Success)
            {
                return new Result
                {
                    Errors = result.Errors
                };
            }
        }

        return new Result
        {
            Success = true
        };
    }

    // Takes the items from the sender
    private async Task<Result> TakeItemsAsync(string userId, string tradeId)
    {
        var tradeItems = await _repository.GetTradeItemsAsync(tradeId, false);
        
        for (int i = 0; i < tradeItems.Length; i++)
        {
            var item = tradeItems[i];

            var result = await _sender.Send(_mapper.AdaptToType<TradeItem, DropInventoryItemCommand>(item, ((string, object))(nameof(DropInventoryItemCommand.UserId), userId), (nameof(DropInventoryItemCommand.Notify), true)));

            if (!result.Success)
            {
                return new Result
                {
                    Errors = result.Errors
                };
            }
        }

        return new Result
        {
            Success = true
        };
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
