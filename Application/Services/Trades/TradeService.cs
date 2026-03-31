using MediatR;
using MapsterMapper;
using Application.Behaviors.Inventories.AddItem;
using Application.Behaviors.Inventories.DropItem;
using Application.Behaviors.Inventories.UnlockItem;
using Application.Behaviors.Trade.CreateTrade;
using Application.Behaviors.Wallet.TakeCash;
using Application.Behaviors.Wallet.GiveCash;
using Application.Behaviors.Trade.RespondTrade;
using Application.Behaviors.Trade.ListTrades;
using Application.Extensions;
using Application.Behaviors.Trade.CancelTrade;
using Application.Behaviors.Trade.GetTrade;
using Application.Behaviors.Identity.GetUsername;
using Application.Behaviors.Item.GetItemName;
using Application.Services.UnitOfWork;
using Application.Repositories;
using Application.Models;
using Application.Results.Trades;
using Application.Models.Trades;
using Domain.Entities.Trades;
using Application.Models.TradeItems;
using Domain.DomainEvents.Trades;
using Application.Behaviors.Inventories.LockItems;
using Domain.Aggregates.Trades;
using Application.Behaviors.Trade.GetTradeItemIds;
using Application.Behaviors.Trade.ItemUsedInTrade;

namespace Application.Services.Trades;

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

    public async Task<Result<TradeOfferResult>> CreateTradeOfferAsync(CreateTradeOfferCommand model)
    {
        if (model is null || string.IsNullOrEmpty(model.SenderUserId) || string.IsNullOrEmpty(model.TargetUserId) || model.Items is null)
            return Result<TradeOfferResult>.Failure("Invalid input data");

        var result = new TradeOfferResult();

        var transactionErrorMessage = await _unitOfWork.ExplicitTransaction(async (TaskCompletionSource<string?> taskCompletionSource) =>
        {
            try
            {
                var lockItemsCommand = new LockItemsCommand
                {
                    UserId = model.SenderUserId,
                    Items = model.Items
                        .Select(x => (x.ItemId, x.Quantity))
                        .ToArray()
                };

                var lockItemsResult = await _sender.Send(lockItemsCommand);

                if (!lockItemsResult.IsSuccess)
                {
                    taskCompletionSource.SetResult(lockItemsResult.Error);

                    return false;
                }

                var items = lockItemsResult.Content!.Items;

                if (items.Length == 0)
                {
                    taskCompletionSource.SetResult("Invalid input data");

                    return false;
                }

                var modelItems = model.Items.ToArray();
                
                for (int i = 0; i < items.Length; i++)
                {
                    var modelItem = modelItems[i];

                    if (modelItem.Price < 0)
                    {
                        taskCompletionSource.SetResult("Invalid price");

                        return false;
                    }

                    var item = items[i];
                    item.Price = modelItem.Price;
                    item.ItemName = await GetItemNameAsync(item.ItemId);
                    item.Quantity = modelItem.Quantity;
                }

                result.Items = items;

                var offer = new Trade(DateTime.UtcNow, model.SenderUserId, model.TargetUserId);

                foreach (var item in items)
                {
                    var tradeContent = new TradeItem(offer.TradeId, item.ItemId, item.Quantity, item.Price);
                    
                    offer.AddTradeContent(tradeContent);
                }

                if (!await _repository.AddEntityAsync(offer))
                {
                    taskCompletionSource.SetResult("Something went wrong");

                    return false;
                }

                result.TradeId = offer.TradeId;
                result.SenderId = model.SenderUserId;
                result.ReceiverId = model.TargetUserId;
                result.CreationDate = offer.SentDate;

                await _repository.SetCacheForTrade(offer);

                return true;
            }
            catch (Exception)
            {
                taskCompletionSource.SetResult("Something went wrong");

                return false;
            }
        });

        if (transactionErrorMessage is not null)
            return Result<TradeOfferResult>.Failure(transactionErrorMessage);

        var receiverUsernameTask = GetUsernameAsync(model.TargetUserId);
        var senderUsernameTask = GetUsernameAsync(model.SenderUserId);

        await Task.WhenAll(
            receiverUsernameTask,
            senderUsernameTask
        );

        result.SenderName = await senderUsernameTask;
        result.ReceiverName = await receiverUsernameTask;

        return Result<TradeOfferResult>.Success(result);
    }

    public async Task<Result<TradeOfferResult>> AcceptTradeOfferAsync(RespondTradeCommand model)
    {
        if (string.IsNullOrEmpty(model.TradeId) || string.IsNullOrEmpty(model.UserId))
            return Result<TradeOfferResult>.Failure("Invalid IDs");

        if (await IsRespondedAsync(model.TradeId))
            return Result<TradeOfferResult>.Failure("Already responded");

        var result = new TradeOfferResult
        {
            TradeId = model.TradeId
        };

        var transactionErrorMessage = await _unitOfWork.ExplicitTransaction(async (TaskCompletionSource<string?> taskCompletionSource) =>
        {
            try
            {
                var trade = await _repository.GetTradeAsync(model.TradeId)!;

                var senderId = trade!.GetSenderId();
                var receiverId = trade.GetReceiverId();

                if (Equals(senderId, model.UserId) || !Equals(receiverId, model.UserId))
                {
                    taskCompletionSource.SetResult("Invalid userId");

                    return false;
                }

                result.SenderId = senderId;
                result.ReceiverId = receiverId;

                var price = trade.GetTotalPrice();

                if (!await _sender.Send(new TakeCashCommand { UserId = model.UserId, Amount = price }))
                {
                    taskCompletionSource.SetResult("Something went wrong");

                    return false;
                }

                result.CreationDate = trade.SentDate;

                var unlockTradeItemsResult = await UnlockTradeItemsAsync(trade);

                if (!unlockTradeItemsResult.IsSuccess)
                {
                    taskCompletionSource.SetResult("Something went wrong");

                    return false;
                }

                var giveItemsResult = await GiveItemsAsync(model.UserId, trade);

                if (!giveItemsResult.IsSuccess)
                {
                    taskCompletionSource.SetResult("Something went wrong");

                    return false;
                }

                var tradeItemDTOs = new List<TradeItemDTO>();

                foreach (var tradeContent in trade.TradeContents)
                {
                    tradeItemDTOs.Add(_mapper.AdaptToType<TradeItem, TradeItemDTO>(tradeContent, (nameof(TradeItemDTO.ItemName), string.Empty)));
                }

                result.Items = tradeItemDTOs;

                var takeItemsResult = await TakeItemsAsync(senderId, trade);

                if (!takeItemsResult.IsSuccess)
                {
                    taskCompletionSource.SetResult("Something went wrong");

                    return false;
                }

                if (!await _sender.Send(new GiveCashCommand { UserId = senderId, Amount = price }))
                {
                    taskCompletionSource.SetResult("Something went wrong");

                    return false;
                }

                var respondTradeResult = await RespondTradeAsync(trade);

                if (!respondTradeResult.IsSuccess)
                {
                    taskCompletionSource.SetResult(respondTradeResult.Error);

                    return false;
                }

                for (int i = 0; i < tradeItemDTOs.Count; i++)
                {
                    string itemName = await GetItemNameAsync(tradeItemDTOs[i].ItemId);

                    tradeItemDTOs[i].ItemName = itemName;
                }

                trade.SetResponse(true);

                var operationResult = await _repository.UpdateEntityAsync(trade);

                if (operationResult)
                {
                    result.Response = trade.Response;
                    result.ResponseDate = trade.ResponseDate;
                }

                return operationResult;
            }
            catch (Exception)
            {
                taskCompletionSource.SetResult("Something went wrong");

                return false;
            }
        });

        if (transactionErrorMessage is not null)
            return Result<TradeOfferResult>.Failure(transactionErrorMessage);

        var senderNameTask = GetUsernameAsync(result.SenderId);
        var receiverNameTask = GetUsernameAsync(result.ReceiverId);

        await Task.WhenAll(
            _publisher.Publish(new TradeRespondedDomainEvent(model.TradeId, result.SenderId, true)),
            senderNameTask,
            receiverNameTask
        );

        result.SenderName = await senderNameTask;
        result.ReceiverName = await receiverNameTask;

        return Result<TradeOfferResult>.Success(result);
    }

    public async Task<Result<TradeOfferResult>> RejectTradeOfferAsync(RespondTradeCommand model)
    {
        if (string.IsNullOrEmpty(model.TradeId) || string.IsNullOrEmpty(model.UserId))
            return Result<TradeOfferResult>.Failure("Invalid IDs");

        if (await IsRespondedAsync(model.TradeId))
            return Result<TradeOfferResult>.Failure("Already responded");
        
        var tradeItemDTOs = new List<TradeItemDTO>();
        var result = new TradeOfferResult
        {
            TradeId = model.TradeId
        };

        var transactionErrorMessage = await _unitOfWork.ExplicitTransaction(async (TaskCompletionSource<string?> taskCompletionSource) =>
        {
            try
            {
                var trade = await _repository.GetTradeAsync(model.TradeId)!;

                var senderId = trade.GetSenderId();
                var receiverId = trade.GetReceiverId();

                if (Equals(senderId, model.UserId) || !Equals(receiverId, model.UserId))
                {
                    taskCompletionSource.SetResult("Invalid userId");

                    return false;
                }

                result.SenderId = senderId;
                result.ReceiverId = receiverId;
                result.CreationDate = trade.SentDate;

                var unlockTradeItemsResult = await UnlockTradeItemsAsync(trade);

                if (!unlockTradeItemsResult.IsSuccess)
                {
                    taskCompletionSource.SetResult("Something went wrong");
                    return false;
                }

                var respondTradeResult = await RespondTradeAsync(trade);

                if (!respondTradeResult.IsSuccess)
                {
                    taskCompletionSource.SetResult(respondTradeResult.Error);
                    return false;
                }

                for (int i = 0; i < tradeItemDTOs.Count; i++)
                {
                    string itemName = await GetItemNameAsync(tradeItemDTOs[i].ItemId);

                    tradeItemDTOs[i].ItemName = itemName;
                }

                trade.SetResponse(false);

                var operationResult = await _repository.UpdateEntityAsync(trade);

                if (operationResult)
                {
                    result.Response = trade.Response;
                    result.ResponseDate = trade.ResponseDate;
                }

                return operationResult;
            }
            catch (Exception)
            {
                taskCompletionSource.SetResult("Something went wrong");
                return false;
            }
        });

        if (transactionErrorMessage is not null)
            return Result<TradeOfferResult>.Failure(transactionErrorMessage);

        var senderNameTask = GetUsernameAsync(result.SenderId);
        var receiverNameTask = GetUsernameAsync(result.ReceiverId);

        await Task.WhenAll(
            _publisher.Publish(new TradeRespondedDomainEvent(model.TradeId, result.SenderId, false)),
            senderNameTask,
            receiverNameTask
        );

        result.SenderName = await senderNameTask;
        result.ReceiverName = await receiverNameTask;
        result.Items = tradeItemDTOs;

        return Result<TradeOfferResult>.Success(result);;
    }

    public async Task<Result<TradeOfferResult>> CancelTradeOfferAsync(CancelTradeCommand model)
    {
        if (string.IsNullOrEmpty(model.TradeId) || string.IsNullOrEmpty(model.UserId))
            return Result<TradeOfferResult>.Failure("Invalid IDs");

        var tradeResponse = await _repository.GetTradeResponseAsync(model.TradeId);

        if (tradeResponse is not null)
            return Result<TradeOfferResult>.Failure("Unable to cancel a trade that already got a response");
        
        var result = new TradeOfferResult
        {
            TradeId = model.TradeId
        };

        var transactionErrorMessage = await _unitOfWork.ExplicitTransaction(async (TaskCompletionSource<string?> taskCompletionSource) =>
        {
            try
            {
                var tmp = await _repository.GetTradeAsync(model.TradeId)!;

                result.SenderId = tmp.GetSenderId();
                result.ReceiverId = tmp.GetReceiverId();
                result.CreationDate = tmp.SentDate;

                if (!Equals(result.SenderId, model.UserId))
                {
                    taskCompletionSource.SetResult("Invalid userId");
                    return false;
                }

                var unlockTradeItemsResult = await UnlockTradeItemsAsync(tmp);

                if (!unlockTradeItemsResult.IsSuccess)
                {
                    taskCompletionSource.SetResult("Something went wrong");
                    return false;
                }

                var tradeItems = new List<TradeItemDTO>();

                foreach (var tradeContent in tmp.TradeContents)
                {
                    var itemName = await GetItemNameAsync(tradeContent.ItemId);
                
                    tradeItems.Add(new TradeItemDTO
                    {
                        ItemId = tradeContent.ItemId,
                        ItemName = itemName,
                        Quantity = tradeContent.Quantity,
                        Price = tradeContent.Price
                    });
                }

                result.Items = tradeItems;

                if (!await _repository.RemoveEntityAsync(tmp))
                {
                    taskCompletionSource.SetResult("Something went wrong");
                    return false;
                }

                return true;
            }
            catch (Exception)
            {
                taskCompletionSource.SetResult("Something went wrong");
                return false;
            }
        });

        if (transactionErrorMessage is not null)
            return Result<TradeOfferResult>.Failure(transactionErrorMessage);

        var senderNameTask = GetUsernameAsync(result.SenderId);
        var receiverNameTask = GetUsernameAsync(result.ReceiverId);

        await Task.WhenAll(
             ClearCacheUsedForTradeAsync(model.TradeId, result.SenderId, result.ReceiverId, result.Items.Select(x => x.ItemId).ToArray()),
             _publisher.Publish(new TradeCancelledDomainEvent(model.TradeId, result.ReceiverId)),
             senderNameTask,
             receiverNameTask
        );

        result.SenderName = await senderNameTask;
        result.ReceiverName = await receiverNameTask;

        return Result<TradeOfferResult>.Success(result);
    }

    public async Task<Result<TradeOffersResult>> GetTradeOffersAsync(ListTradesQuery model)
    {
        if (string.IsNullOrEmpty(model.UserId))
            return Result<TradeOffersResult>.Failure("Invalid input data");

        var sentTradeOfferIds = Array.Empty<string>();
        var receivedTradeOfferIds = Array.Empty<string>();

        if (model.TradeDirection == TradeDirection.All || model.TradeDirection == TradeDirection.Sent)
            sentTradeOfferIds = await GetSentTradeOffersIdListAsync(model.UserId, model.TradeItemIds, model.Responded);

        if (model.TradeDirection == TradeDirection.All || model.TradeDirection == TradeDirection.Received)
            receivedTradeOfferIds = await GetReceivedTradeOffersIdListAsync(model.UserId, model.TradeItemIds, model.Responded);

        if (sentTradeOfferIds is null || receivedTradeOfferIds is null)
            return Result<TradeOffersResult>.Failure("Something went wrong");
        
        return Result<TradeOffersResult>.Success(new TradeOffersResult
        {
            SentTradeOfferIds = sentTradeOfferIds,
            ReceivedTradeOfferIds = receivedTradeOfferIds
        });
    }

    public async Task<Result<TradeOfferResult>> GetTradeOfferAsync(RequestTradeOfferQuery requestTradeOffer)
    {
        if (requestTradeOffer is null || string.IsNullOrEmpty(requestTradeOffer.TradeId))
            return Result<TradeOfferResult>.Failure("Invalid input data");

        var trade = await _repository.GetTradeAsync(requestTradeOffer.TradeId);

        if (trade is null)
            return Result<TradeOfferResult>.Failure("Something went wrong");

        string senderId = trade.GetSenderId();
        string receiverId = trade.GetReceiverId();

        var senderNameTask = GetUsernameAsync(senderId);
        var receiverNameTask = GetUsernameAsync(receiverId);

        await Task.WhenAll(
            senderNameTask,
            receiverNameTask
        );

        var tradeItemsData = new TradeItemDTO[trade.TradeContents.Count];

        int i = 0;

        foreach (var tradeItem in trade.TradeContents)
        {
            string itemName = await GetItemNameAsync(tradeItem.ItemId);

            tradeItemsData[i] = _mapper.AdaptToType<TradeItem, TradeItemDTO>(tradeItem, (nameof(TradeItemDTO.ItemName), itemName));
        
            i++;
        }

        return Result<TradeOfferResult>.Success(new TradeOfferResult
        {
            TradeId = trade.TradeId,
            SenderId = senderId,
            SenderName = await senderNameTask,
            ReceiverId = receiverId,
            ReceiverName = await receiverNameTask,
            CreationDate = trade.SentDate,
            Response = trade.Response,
            ResponseDate = trade.ResponseDate,
            Items = tradeItemsData
        });
    }

    public async Task<Result<string[]>> GetItemTradeIdsAsync(GetTradesUsingTheItemQuery model)
    {
        string[] resultArray; 

        if (string.IsNullOrEmpty(model.ItemId))
            resultArray = Array.Empty<string>();
        else
            resultArray = await _repository.GetTradeIdsUsingItemAsync(model.ItemId);

        return Result<string[]>.Success(resultArray);
    }

    public async Task<bool> IsItemUsedInTrade(ItemUsedInTradeQuery model)
    {
        if (model is null || string.IsNullOrEmpty(model.ItemId))
            return false;

        return await _repository.IsItemUsedInTrade(model.ItemId);
    }

    public void Dispose()
    {
        _repository.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task<Result> RespondTradeAsync(Trade trade)
    {
        // move trade items to the trade content history
        var moveTradeItemsResultStatus = await _repository.MoveTradeContentToHistory(trade.TradeId);

        if (!moveTradeItemsResultStatus)
            return Result.Failure("Something went wrong while moving the trade items to the history");

        // clear the trade content
        trade.ClearTradeContents();
        var clarTradeContentResult = await _repository.SaveChangesAsync() > 0;

        if (!clarTradeContentResult)
            return Result.Failure("Something went wrong");

        return Result.Success();
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
            await FilterTradesByTradeItemsAsync(tradeIds, tradeItems, remainedTradeIds, responded);

            tradeIds = remainedTradeIds;
        }

        return tradeIds.ToArray();
    }

    private async Task FilterTradesByTradeItemsAsync(List<string> tradeIds, string[] tradeItems, List<string> remainedTradeIds, bool responded = false)
    {
        foreach (var tradeId in tradeIds)
        {
            bool keepTrade = false;

            for (int i = 0; i < tradeItems.Length; i++)
            {
                keepTrade = await _repository.HasTradeItem(tradeId, tradeItems[i], responded);

                if (keepTrade)
                {
                    break;
                }
            }

            if (keepTrade) remainedTradeIds.Add(tradeId);
        }
    }
    
    private async Task<bool> IsRespondedAsync(string tradeId)
    {
        var tradeResponse = await _repository.GetTradeResponseAsync(tradeId);

        return tradeResponse is not null;
    }

    private async Task<Result> UnlockTradeItemsAsync(Trade trade)
    {
        var tradeItems = trade.TradeContents;

        foreach (var item in tradeItems)
        {
            if (item is null)
                continue;

            var request = new UnlockItemCommand
            {
                UserId = trade.GetSenderId(),
                ItemId = item.ItemId,
                Quantity = item.Quantity,
                Notify = true
            };

            var result = await _sender.Send(request);

            if (!result.IsSuccess)
            {
                return Result.Failure(result.Error!);
            }
        }

        return Result.Success();
    }

    // Takes the items from trade to the receiver
    private async Task<Result> GiveItemsAsync(string userId, Trade trade)
    {
        var tradeItems = trade.TradeContents;
        
        foreach (var item in tradeItems)
        {
            var request = new AddInventoryItemCommand
            {
                UserId = userId,
                ItemId = item.ItemId,
                Quantity = item.Quantity,
                Notify = true
            };

            var result = await _sender.Send(request);
            
            if (!result.IsSuccess)
            {
                return Result.Failure(result.Error!);
            }
        }

        return Result.Success();
    }

    // Takes the items from the sender
    private async Task<Result> TakeItemsAsync(string userId, Trade trade)
    {
        var tradeItems = trade.TradeContents;

        foreach (var item in tradeItems)
        {
            var request = new DropInventoryItemCommand
            {
                UserId = userId,
                ItemId = item.ItemId,
                Quantity = item.Quantity,
                Notify = true
            };

            var result = await _sender.Send(request);

            if (!result.IsSuccess)
            {
                return Result.Failure(result.Error!);
            }
        }

        return Result.Success();
    }
    
    private Task<string> GetItemNameAsync(string itemId) => _sender.Send(new GetItemNameQuery { ItemId = itemId });

    private Task<string> GetUsernameAsync(string userId) => _sender.Send(new GetUsernameQuery { UserId = userId });
}
