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
using Application.Repositories;
using Application.Results.Trades;
using Application.Models.Trades;
using Domain.Entities.Trades;
using Application.Models.TradeItems;
using Domain.DomainEvents.Trades;
using Application.Behaviors.Inventories.LockItems;
using Domain.Aggregates.Trades;
using Application.Behaviors.Trade.GetTradeItemIds;
using Application.Behaviors.Trade.ItemUsedInTrade;
using Microsoft.Extensions.Logging;
using Application.Results.Inventories;

namespace Application.Services.Trades;

public class TradeService : ITradeService
{
    private readonly ILogger<TradeService> _logger;
    private readonly ICachedTradeRepository _repository;
    private readonly ISender _sender;
    private readonly IPublisher _publisher;
    private readonly IMapper _mapper;

    public TradeService(ILogger<TradeService> logger, ICachedTradeRepository tradeRepository, ISender sender, IPublisher publisher, IMapper mapper)
    {
        _logger = logger;
        _repository = tradeRepository;
        _sender = sender;
        _publisher = publisher;
        _mapper = mapper;
    }

    public async Task<Result<TradeOfferResult>> CreateTradeOfferAsync(CreateTradeOfferCommand model, CancellationToken cancellationToken)
    {
        if (model is null || string.IsNullOrEmpty(model.SenderUserId) || string.IsNullOrEmpty(model.TargetUserId) || model.Items is null)
            return Result<TradeOfferResult>.Failure("Invalid input data");

        var result = new TradeOfferResult();

        await _repository.StartTransactionAsync(cancellationToken);

        try
        {
            var lockItemsResult = await LockTradeItemsAsync(
                model.SenderUserId,
                model.Items
                    .Select(x => (x.ItemId, x.Quantity))
                    .ToArray()
            );

            if (!lockItemsResult.IsSuccess)
            {
                await _repository.RollbackTransactionAsync(cancellationToken);
                _logger.LogWarning("Failed creating a trade from sender with id {senderId}. Unable to lock trade items", model.SenderUserId);
                return Result<TradeOfferResult>.Failure("Something went wrong");
            }

            var items = lockItemsResult.Content!.Items;

            if (items.Length == 0)
            {
                await _repository.RollbackTransactionAsync(cancellationToken);
                _logger.LogWarning("Failed creating a trade from sender with id {senderId}. Trade items not provided", model.SenderUserId);
                return Result<TradeOfferResult>.Failure("Something went wrong");
            }

            var modelItems = model.Items.ToArray();
            
            for (int i = 0; i < items.Length; i++)
            {
                var modelItem = modelItems[i];

                if (modelItem.Price < 0)
                {
                    await _repository.RollbackTransactionAsync(cancellationToken);
                    _logger.LogWarning("Failed creating a trade from sender with id {senderId}. Invalid price", model.SenderUserId);
                    return Result<TradeOfferResult>.Failure("Something went wrong");
                }

                var item = items[i];
                item.Price = modelItem.Price;
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
                await _repository.RollbackTransactionAsync(cancellationToken);
                _logger.LogWarning("Failed creating a trade from sender with id {senderId}. Unable to add the trade entity", model.SenderUserId);
                return Result<TradeOfferResult>.Failure("Something went wrong");
            }

            result.TradeId = offer.TradeId;
            result.SenderId = model.SenderUserId;
            result.ReceiverId = model.TargetUserId;
            result.CreationDate = offer.SentDate;

            await _repository.SetCacheForTrade(offer);

            await _repository.SaveChangesAsync();
            await _repository.CommitTransactionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await _repository.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Something went wrong while creating from sender with id {senderId}", model.SenderUserId);
            return Result<TradeOfferResult>.Failure("Something went wrong");
        }
        
        await ApplyTradeItemsNameToResult(result);

        return await ApplyUsernameDataToResultAndValidate(result, model.SenderUserId, model.TargetUserId);
    }

    public async Task<Result<TradeOfferResult>> AcceptTradeOfferAsync(RespondTradeCommand model, CancellationToken cancellationToken)
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

        await _repository.StartTransactionAsync(cancellationToken);        

        try
        {
            var trade = await _repository.GetTradeAsync(model.TradeId)!;

            if (trade is null)
            {
                await _repository.RollbackTransactionAsync(cancellationToken);
                _logger.LogWarning("Failed accepting trade with id {tradeId}. Invalid trade id, trade doesn't exist", model.TradeId);
                return Result<TradeOfferResult>.Failure("Invalid trade id. Trade doesn't exist");
            }

            var senderId = trade!.GetSenderId();
            var receiverId = trade.GetReceiverId();

            if (Equals(senderId, model.UserId) || !Equals(receiverId, model.UserId))
            {
                await _repository.RollbackTransactionAsync(cancellationToken);
                _logger.LogWarning("Failed accepting trade with id {tradeId}. Invalid user id. The provided sender id does not match with the sender of the given trade", model.TradeId);
                return Result<TradeOfferResult>.Failure("Invalid user id");
            }

            result.SenderId = senderId;
            result.ReceiverId = receiverId;

            var price = trade.GetTotalPrice();

            if (!await _sender.Send(new TakeCashCommand { UserId = model.UserId, Amount = price }))
            {
                await _repository.RollbackTransactionAsync(cancellationToken);
                _logger.LogWarning("Failed handling the cash transaction between traders of trade with id {tradeId}", trade.TradeId);
                return Result<TradeOfferResult>.Failure("Something went wrong");
            }

            result.CreationDate = trade.SentDate;

            var unlockTradeItemsResult = await UnlockTradeItemsAsync(trade);

            if (!unlockTradeItemsResult.IsSuccess)
            {
                await _repository.RollbackTransactionAsync(cancellationToken);
                _logger.LogWarning("Failed unlocking trade items of trade with id {tradeId}", trade.TradeId);
                return Result<TradeOfferResult>.Failure("Something went wrong");
            }

            var giveItemsResult = await GiveItemsAsync(model.UserId, trade);

            if (!giveItemsResult.IsSuccess)
            {
                await _repository.RollbackTransactionAsync(cancellationToken);
                _logger.LogWarning("Failed sending trade items of trade with id {tradeId} to the receiver with id {ReceiverId}", trade.TradeId, trade.ReceiverId);
                return Result<TradeOfferResult>.Failure("Something went wrong");
            }

            foreach (var tradeContent in trade.TradeContents)
            {
                tradeItemDTOs.Add(_mapper.AdaptToType<TradeItem, TradeItemDTO>(tradeContent, (nameof(TradeItemDTO.ItemName), string.Empty)));
            }

            var takeItemsResult = await TakeItemsAsync(senderId, trade);

            if (!takeItemsResult.IsSuccess)
            {
                await _repository.RollbackTransactionAsync(cancellationToken);
                _logger.LogWarning("Failed taking trade items of trade with id {tradeId} from the sender with id {SenderId}", trade.TradeId, trade.SenderId);
                return Result<TradeOfferResult>.Failure("Something went wrong");
            }

            if (!await _sender.Send(new GiveCashCommand { UserId = senderId, Amount = price }))
            {
                await _repository.RollbackTransactionAsync(cancellationToken);
                _logger.LogWarning("Failed handling the cash transaction between traders of trade with id {tradeId}", trade.TradeId);
                return Result<TradeOfferResult>.Failure("Something went wrong");
            }

            var respondTradeResult = await RespondTradeAsync(trade);

            if (!respondTradeResult.IsSuccess)
            {
                await _repository.RollbackTransactionAsync(cancellationToken);
                _logger.LogWarning("Failed accepting trade with id {tradeId}", trade.TradeId);
                return Result<TradeOfferResult>.Failure("Something went wrong");
            }

            trade.SetResponse(true);

            var operationResult = await _repository.UpdateEntityAsync(trade);

            if (operationResult)
            {
                result.Response = trade.Response;
                result.ResponseDate = trade.ResponseDate;
            }

            await _repository.SaveChangesAsync();
            await _repository.CommitTransactionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await _repository.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Something went wrong while accepting a trade with id {tradeId}", model.TradeId);
            return Result<TradeOfferResult>.Failure("Something went wrong");
        }

        result.Items = tradeItemDTOs;

        await ApplyTradeItemsNameToResult(result);

        return await ApplyUsernameDataToResultAndValidate(result, result.SenderId, result.ReceiverId);
    }

    public async Task<Result<TradeOfferResult>> RejectTradeOfferAsync(RespondTradeCommand model, CancellationToken cancellationToken)
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

        await _repository.StartTransactionAsync(cancellationToken);

        try
        {
            var trade = await _repository.GetTradeAsync(model.TradeId);

            if (trade is null)
            {
                await _repository.RollbackTransactionAsync(cancellationToken);
                _logger.LogWarning("Failed rejecting trade with id {tradeId}. Invalid trade id, trade doesn't exist", model.TradeId);
                return Result<TradeOfferResult>.Failure("Invalid trade id. Trade doesn't exist");
            }

            var senderId = trade.GetSenderId();
            var receiverId = trade.GetReceiverId();

            if (Equals(senderId, model.UserId) || !Equals(receiverId, model.UserId))
            {
                await _repository.RollbackTransactionAsync(cancellationToken);
                _logger.LogWarning("Failed rejecting trade with id {tradeId}. Invalid user id. The provided sender id does not match with the sender of the given trade", model.TradeId);
                return Result<TradeOfferResult>.Failure("Invalid user id");
            }

            result.SenderId = senderId;
            result.ReceiverId = receiverId;
            result.CreationDate = trade.SentDate;

            var unlockTradeItemsResult = await UnlockTradeItemsAsync(trade);

            if (!unlockTradeItemsResult.IsSuccess)
            {
                await _repository.RollbackTransactionAsync(cancellationToken);
                _logger.LogWarning("Failed unlocking trade items of trade with id {tradeId}", trade.TradeId);
                return Result<TradeOfferResult>.Failure("Something went wrong");
            }

            var respondTradeResult = await RespondTradeAsync(trade);

            if (!respondTradeResult.IsSuccess)
            {
                await _repository.RollbackTransactionAsync(cancellationToken);
                _logger.LogWarning("Failed rejecting trade with id {tradeId}", trade.TradeId);
                return Result<TradeOfferResult>.Failure("Something went wrong");
            }

            trade.SetResponse(false);

            var operationResult = await _repository.UpdateEntityAsync(trade);

            if (operationResult)
            {
                result.Response = trade.Response;
                result.ResponseDate = trade.ResponseDate;
            }

            await _repository.SaveChangesAsync();
            await _repository.CommitTransactionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await _repository.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Something went wrong while rejecting a trade with id {tradeId}", model.TradeId);
            return Result<TradeOfferResult>.Failure("Something went wrong");
        }

        result.Items = tradeItemDTOs;
        
        await ApplyTradeItemsNameToResult(result);

        return await ApplyUsernameDataToResultAndValidate(result, result.SenderId, result.ReceiverId);
    }

    public async Task<Result<TradeOfferResult>> CancelTradeOfferAsync(CancelTradeCommand model, CancellationToken cancellationToken)
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

        await _repository.StartTransactionAsync(cancellationToken);

        try
        {
            var trade = await _repository.GetTradeAsync(model.TradeId)!;

            if (trade is null)
            {
                await _repository.RollbackTransactionAsync(cancellationToken);
                _logger.LogWarning("Failed canceling trade with id {tradeId}. Invalid trade id, trade doesn't exist", model.TradeId);
                return Result<TradeOfferResult>.Failure("Invalid trade id. Trade doesn't exist");
            }

            result.SenderId = trade.GetSenderId();
            result.ReceiverId = trade.GetReceiverId();
            result.CreationDate = trade.SentDate;

            if (!Equals(result.SenderId, model.UserId))
            {
                await _repository.RollbackTransactionAsync(cancellationToken);
                _logger.LogWarning("Failed canceling trade with id {tradeId}. The provided sender id does not match with the sender of the given trade", trade.TradeId);
                return Result<TradeOfferResult>.Failure("Invalid userId");
            }

            var unlockTradeItemsResult = await UnlockTradeItemsAsync(trade);

            if (!unlockTradeItemsResult.IsSuccess)
            {
                await _repository.RollbackTransactionAsync(cancellationToken);
                _logger.LogWarning("Failed unlocking trade items of trade with id {tradeId}", trade.TradeId);
                return Result<TradeOfferResult>.Failure("Something went wrong");
            }

            var tradeItems = new List<TradeItemDTO>();

            foreach (var tradeContent in trade.TradeContents)
            {
                tradeItems.Add(new TradeItemDTO
                {
                    ItemId = tradeContent.ItemId,
                    Quantity = tradeContent.Quantity,
                    Price = tradeContent.Price
                });
            }

            result.Items = tradeItems;

            if (!await _repository.RemoveEntityAsync(trade))
            {
                await _repository.RollbackTransactionAsync(cancellationToken);
                _logger.LogWarning("Failed deleting trade with id {tradeId}", trade.TradeId);
                return Result<TradeOfferResult>.Failure("Something went wrong");
            }

            await _repository.SaveChangesAsync();
            await _repository.CommitTransactionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await _repository.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Something went wrong while canceling a trade with id {tradeId}", model.TradeId);
            return Result<TradeOfferResult>.Failure("Something went wrong");
        }

        await Task.WhenAll(
            ClearCacheUsedForTradeAsync(model.TradeId, result.SenderId, result.ReceiverId, result.Items.Select(x => x.ItemId).ToArray()),
            _publisher.Publish(new TradeCancelledDomainEvent(model.TradeId, result.ReceiverId))
        );

        await ApplyTradeItemsNameToResult(result);

        return await ApplyUsernameDataToResultAndValidate(result, result.SenderId, result.ReceiverId);
    }

    public async Task<Result<TradeOffersResult>> GetTradeOffersAsync(ListTradesQuery model, CancellationToken cancellationToken)
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

    public async Task<Result<TradeOfferResult>> GetTradeOfferAsync(RequestTradeOfferQuery requestTradeOffer, CancellationToken cancellationToken)
    {
        if (requestTradeOffer is null || string.IsNullOrEmpty(requestTradeOffer.TradeId))
            return Result<TradeOfferResult>.Failure("Invalid input data");

        var trade = await _repository.GetTradeAsync(requestTradeOffer.TradeId);

        if (trade is null)
            return Result<TradeOfferResult>.Failure("Something went wrong");

        string senderId = trade.GetSenderId();
        string receiverId = trade.GetReceiverId();

        var result = new TradeOfferResult
        {
            TradeId = trade.TradeId,
            SenderId = senderId,
            ReceiverId = receiverId,
            CreationDate = trade.SentDate,
            Response = trade.Response,
            ResponseDate = trade.ResponseDate
        };

        await ApplyTradeItemsNameToResult(result, trade.TradeContents);

        return await ApplyUsernameDataToResultAndValidate(result, senderId, receiverId);
    }

    public async Task<Result<string[]>> GetItemTradeIdsAsync(GetTradesUsingTheItemQuery model, CancellationToken cancellationToken)
    {
        string[] resultArray;

        if (string.IsNullOrEmpty(model.ItemId))
            resultArray = Array.Empty<string>();
        else
            resultArray = await _repository.GetTradeIdsUsingItemAsync(model.ItemId);

        return Result<string[]>.Success(resultArray);
    }

    public async Task<bool> IsItemUsedInTrade(ItemUsedInTradeQuery model, CancellationToken cancellationToken)
    {
        if (model is null || string.IsNullOrEmpty(model.ItemId))
            return false;

        return await _repository.IsItemUsedInTrade(model.ItemId);
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

    private async Task<Result<LockItemsResult>> LockTradeItemsAsync(string senderUserId, (string ItemId, int Quantity)[] items)
    {
        var lockItemsCommand = new LockItemsCommand
        {
            UserId = senderUserId,
            Items = items
        };

        return await _sender.Send(lockItemsCommand);
    }

    private async Task<Result> UnlockTradeItemsAsync(Trade trade)
    {
        var tradeItems = trade.TradeContents.ToArray();
        var request = new UnlockItemsCommand
        {
            UserId = trade.GetSenderId(),
            Items = new (string itemId, int quantity)[tradeItems.Length],
            Notify = true
        };

        for (int i = 0; i < tradeItems.Length; i++)
        {
            var item = tradeItems[i];

            request.Items[i] = (item.ItemId, item.Quantity);
        }

        var result = await _sender.Send(request);

        if (!result.IsSuccess)
        {
            return Result.Failure(result.Error!);
        }

        return Result.Success();
    }

    private async Task<Result> GiveItemsAsync(string userId, Trade trade)
    {
        var tradeItems = trade.TradeContents.ToArray();
        var itemsCount = tradeItems.Length;
        
        var request = new AddInventoryItemsCommand
        {
            UserId = userId,
            Items = new (string itemId, int quantity)[itemsCount],
            Notify = true
        };

        for (int i = 0; i < itemsCount; i++)
        {
            var item = tradeItems[i];

            request.Items[i] = (item.ItemId, item.Quantity);
        }

        var result = await _sender.Send(request);

        if (!result.IsSuccess)
        {
            return Result.Failure(result.Error!);
        }

        return Result.Success();
    }

    // Takes the items from the sender
    private async Task<Result> TakeItemsAsync(string userId, Trade trade)
    {
        var tradeItems = trade.TradeContents.ToArray();
        var itemsCount = tradeItems.Length;

        var request = new DropInventoryItemsCommand
        {
            UserId = userId,
            Items = new (string itemId, int quantity)[itemsCount],
            Notify = true
        };

        for (int i = 0; i < itemsCount; i++)
        {
            var item = tradeItems[i];

            request.Items[i] = (item.ItemId, item.Quantity);
        }

        var result = await _sender.Send(request);

        if (!result.IsSuccess)
        {
            return Result.Failure(result.Error!);
        }

        return Result.Success();
    }

    private async Task ApplyTradeItemsNameToResult(TradeOfferResult result)
    {
        var tradeItems = result.Items.ToArray();
        var tradeItemsId = tradeItems
            .Select(tradeItem => tradeItem.ItemId)
            .ToArray();
        var tradeItemsName = await GetItemsNameAsync(tradeItemsId);

        for (int i = 0; i < tradeItemsId.Length; i++)
        {
            string itemId = tradeItems[i].ItemId;

            if (!tradeItemsName.TryGetValue(itemId, out var itemName) || string.IsNullOrEmpty(itemName))
            {
                _logger.LogWarning("Missing item name for the item with id {itemId}", itemId);
                continue;
            }

            tradeItems[i].ItemName = itemName;
        }
    }

    private async Task ApplyTradeItemsNameToResult(TradeOfferResult result, IEnumerable<TradeItem> tradeItems)
    {
        var resultItems = new List<TradeItemDTO>();

        var tradeItemsArr = tradeItems.ToArray();
        var tradeItemsId = tradeItemsArr
            .Select(tradeItem => tradeItem.ItemId)
            .ToArray();
        var tradeItemsName = await GetItemsNameAsync(tradeItemsId);

        for (int i = 0; i < tradeItemsId.Length; i++)
        {
            var tradeItem = tradeItemsArr[i];
            string itemId = tradeItem.ItemId;

            if (!tradeItemsName.TryGetValue(itemId, out var itemName) || string.IsNullOrEmpty(itemName))
            {
                _logger.LogWarning("Missing item name for the item with id {itemId}", itemId);
                continue;
            }

            resultItems.Add(_mapper.AdaptToType<TradeItem, TradeItemDTO>(tradeItem, (nameof(TradeItemDTO.ItemName), itemName)));
        }

        result.Items = resultItems;
    }

    private async Task<Result<TradeOfferResult>> ApplyUsernameDataToResultAndValidate(TradeOfferResult result, string senderUserId, string targetUserId)
    {
        var usernames = await GetUsernamesAsync([targetUserId, senderUserId]);

        return await ApplyUsernameDataToResultAndValidate(result, senderUserId, targetUserId, usernames);
    }

    private async Task<Result<TradeOfferResult>> ApplyUsernameDataToResultAndValidate(TradeOfferResult result, string senderUserId, string targetUserId, Dictionary<string, string> usernames)
    {
        if (!SetUsernameToProperty(usernames, senderUserId, username => result.SenderName = username))
            return Result<TradeOfferResult>.Failure("Unable to retrieve the trade sender username");
        if (!SetUsernameToProperty(usernames, targetUserId, username => result.ReceiverName = username))
            return Result<TradeOfferResult>.Failure("Unable to retrieve the trade receiver username");
        
        return Result<TradeOfferResult>.Success(result);
    }

    private static bool SetUsernameToProperty(Dictionary<string, string> source, string userId, Action<string> setUsername)
    {
        if (source.TryGetValue(userId, out string? username))
        {
            if (string.IsNullOrEmpty(username))
            {
                return false;
            }
            else
            {
                setUsername(username);
            }    
        }

        return true;
    }

    private Task<Dictionary<string, string>> GetItemsNameAsync(string[] itemIds) => _sender.Send(new GetItemsNameQuery { ItemIds = itemIds });

    private async Task<Dictionary<string, string>> GetUsernamesAsync(string[] userIds) => (await _sender.Send(new GetUsernamesQuery { UserIds = userIds })).Content ?? new Dictionary<string, string>();
}
