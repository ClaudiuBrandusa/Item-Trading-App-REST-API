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

namespace Item_Trading_App_REST_API.Services.Trade
{
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
            {
                return new SentTradeOffer
                {
                    Errors = new[] { "Invalid input data" }
                };
            }

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
            {
                return new SentTradeOffer
                {
                    Errors = new[] { "Invalid input data" }
                };
            }

            var offer = new Entities.Trade
            {
                TradeId = Guid.NewGuid().ToString(),
                SentDate = DateTime.Now
            };

            _context.Trades.Add(offer);

            await _context.SaveChangesAsync();

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

            _context.SentTrades.Add(new Entities.SentTrade { TradeId = offer.TradeId, SenderId = model.SenderUserId });

            await _context.SaveChangesAsync();

            _context.ReceivedTrades.Add(new Entities.ReceivedTrade { TradeId = offer.TradeId, ReceiverId = model.TargetUserId });

            await _context.SaveChangesAsync();
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
            await _notificationService.SendCreatedNotificationToUserAsync(model.TargetUserId, NotificationCategoryTypes.Trade, offer.TradeId);

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

        public async Task<AcceptTradeOfferResult> AcceptTradeOffer(string tradeOfferId, string userId)
        {
            if (string.IsNullOrEmpty(tradeOfferId) || string.IsNullOrEmpty(userId))
            {
                return new AcceptTradeOfferResult
                {
                    Errors = new[] { "Invalid IDs" }
                };
            }

            string receiverId = await GetReceiverId(tradeOfferId);

            if (!Equals(receiverId, userId))
            {
                return new AcceptTradeOfferResult
                {
                    Errors = new[] { "Invalid userId" }
                };
            }

            var entity = await _cacheService.GetCacheValueAsync<CachedTrade>(GetTradesCacheKey(tradeOfferId));

            if (entity is null)
            {
                var tmp = GetTradeOfferEntity(tradeOfferId);

                if (tmp is null)
                    return new AcceptTradeOfferResult
                    {
                        Errors = new[] { "Something went wrong" }
                    };

                entity = new CachedTrade
                {
                    TradeId = tmp.TradeId,
                    ReceiverUserId = receiverId,
                    SentDate = tmp.SentDate,
                    ResponseDate = tmp.ResponseDate,
                    Response = tmp.Response
                };
            }

            if (entity.Response is not null)
            {
                return new AcceptTradeOfferResult
                {
                    Errors = new[] { "Already responded" }
                };
            }

            int price = await GetTotalPrice(tradeOfferId);

            if (price > await _mediator.Send(new GetUserCashQuery { UserId = userId }))
            {
                return new AcceptTradeOfferResult
                {
                    Errors = new[] { "User has not enough money" }
                };
            }

            if (!await _mediator.Send(new TakeCashQuery { UserId = userId, Amount = price }))
            {
                return new AcceptTradeOfferResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            string senderId = await GetSenderId(tradeOfferId);
            entity.SenderUserId = senderId;

            if (!await UnlockTradeItemsAsync(senderId, tradeOfferId))
            {
                return new AcceptTradeOfferResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            if (!await GiveItemsAsync(userId, tradeOfferId))
            {
                return new AcceptTradeOfferResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }


            if (!await TakeItemsAsync(senderId, tradeOfferId))
            {
                return new AcceptTradeOfferResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            if (!await _mediator.Send(new GiveCashQuery { UserId = senderId, Amount = price }))
            {
                return new AcceptTradeOfferResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            entity.Response = true;
            entity.ResponseDate = DateTime.Now;

            _context.Trades.Update(new Entities.Trade { TradeId = tradeOfferId, Response = entity.Response, ResponseDate = entity.ResponseDate, SentDate = entity.SentDate });

            await _context.SaveChangesAsync();
            await _cacheService.SetCacheValueAsync(GetTradesCacheKey(tradeOfferId), entity);
            await _notificationService.SendUpdatedNotificationToUserAsync(senderId, NotificationCategoryTypes.Trade, tradeOfferId, new RespondedTradeNotification
            {
                Response = entity.Response
            });

            return new AcceptTradeOfferResult
            {
                TradeOfferId = tradeOfferId,
                SenderId = senderId,
                SenderName = await GetUsername(senderId),
                ReceivedDate = entity.SentDate,
                ResponseDate = (DateTime)entity.ResponseDate,
                Success = true
            };
        }

        public async Task<RejectTradeOfferResult> RejectTradeOffer(string tradeOfferId, string userId)
        {
            if (string.IsNullOrEmpty(tradeOfferId) || string.IsNullOrEmpty(userId))
            {
                return new RejectTradeOfferResult
                {
                    Errors = new[] { "Invalid IDs" }
                };
            }

            string receiverId = await GetReceiverId(tradeOfferId);

            if (!Equals(receiverId, userId))
            {
                return new RejectTradeOfferResult
                {
                    Errors = new[] { "Invalid userId" }
                };
            }

            var entity = await _cacheService.GetCacheValueAsync<CachedTrade>(GetTradesCacheKey(tradeOfferId));

            if (entity is null)
            {
                var tmp = GetTradeOfferEntity(tradeOfferId);

                if (tmp is null)
                    return new RejectTradeOfferResult
                    {
                        Errors = new[] { "Something went wrong" }
                    };

                entity = new CachedTrade
                {
                    TradeId = tmp.TradeId,
                    ReceiverUserId = receiverId,
                    SentDate = tmp.SentDate,
                    ResponseDate = tmp.ResponseDate,
                    Response = tmp.Response
                };
            }

            if (entity.Response is not null)
            {
                return new RejectTradeOfferResult
                {
                    Errors = new[] { "Already responded" }
                };
            }

            string senderId = await GetSenderId(tradeOfferId);
            entity.SenderUserId = senderId;

            if (!await UnlockTradeItemsAsync(senderId, tradeOfferId))
            {
                return new RejectTradeOfferResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            entity.Response = false;
            entity.ResponseDate = DateTime.Now;

            _context.Trades.Update(new Entities.Trade { TradeId = tradeOfferId, Response = entity.Response, ResponseDate = entity.ResponseDate, SentDate = entity.SentDate });

            await _context.SaveChangesAsync();
            await _cacheService.SetCacheValueAsync(GetTradesCacheKey(tradeOfferId), entity);
            await _notificationService.SendUpdatedNotificationToUserAsync(senderId, NotificationCategoryTypes.Trade, tradeOfferId, new RespondedTradeNotification
            {
                Response = entity.Response
            });

            return new RejectTradeOfferResult
            {
                TradeOfferId = tradeOfferId,
                SenderId = senderId,
                SenderName = await GetUsername(senderId),
                ReceivedDate = entity.SentDate,
                ResponseDate = (DateTime)entity.ResponseDate,
                Success = true
            };
        }

        public async Task<CancelTradeOfferResult> CancelTradeOffer(string tradeOfferId, string userId)
        {
            if (string.IsNullOrEmpty(tradeOfferId) || string.IsNullOrEmpty(userId))
            {
                return new CancelTradeOfferResult
                {
                    Errors = new[] { "Invalid IDs" }
                };
            }

            string senderId = await GetSenderId(tradeOfferId);

            if (!Equals(senderId, userId))
            {
                return new CancelTradeOfferResult
                {
                    Errors = new[] { "Invalid userId" }
                };
            }

            var entity = await _cacheService.GetCacheValueAsync<CachedTrade>(GetTradesCacheKey(tradeOfferId));

            if (entity is null)
            {
                var tmp = GetTradeOfferEntity(tradeOfferId);

                if (tmp is null)
                    return new CancelTradeOfferResult
                    {
                        Errors = new[] { "Something went wrong" }
                    };

                entity = new CachedTrade
                {
                    TradeId = tmp.TradeId,
                    SenderUserId = userId,
                    SentDate = tmp.SentDate,
                    ResponseDate = tmp.ResponseDate,
                    Response = tmp.Response
                };
            }

            if (entity.Response.HasValue)
            {
                return new CancelTradeOfferResult
                {
                    Errors = new[] { "Unable to cancel a trade that already got a response" }
                };
            }

            if (!await UnlockTradeItemsAsync(userId, tradeOfferId))
            {
                return new CancelTradeOfferResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            string receiverId = await GetReceiverId(tradeOfferId);

            entity.ReceiverUserId = receiverId;

            _context.Trades.Remove(new Entities.Trade { TradeId = tradeOfferId });
            int removed = await _context.SaveChangesAsync();

            if (removed == 0)
            {
                return new CancelTradeOfferResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            await _cacheService.ClearCacheKeyAsync(GetTradesCacheKey(tradeOfferId));
            await _cacheService.ClearCacheKeyAsync(GetSentTradesCacheKey(senderId, tradeOfferId));
            await _cacheService.ClearCacheKeyAsync(GetReceivedTradesCacheKey(receiverId, tradeOfferId));
            await _notificationService.SendUpdatedNotificationToUserAsync(receiverId, NotificationCategoryTypes.Trade, tradeOfferId, new RespondedTradeNotification
            {
                Response = entity.Response
            });

            return new CancelTradeOfferResult
            {
                TradeOfferId = tradeOfferId,
                ReceiverId = receiverId,
                ReceiverName = await GetUsername(receiverId),
                Success = true
            };
        }

        public async Task<TradeOffersResult> GetReceivedTradeOffers(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return new TradeOffersResult
                {
                    Errors = new[] { "Invalid user ID" }
                };
            }

            var idList = await GetReceivedTradeOffersIdList(userId);

            if (idList is null)
            {
                return new TradeOffersResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            return new TradeOffersResult
            {
                TradeOffers = idList,
                Success = true
            };
        }

        public async Task<TradeOffersResult> GetReceivedRespondedTradeOffers(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return new TradeOffersResult
                {
                    Errors = new[] { "Invalid user ID" }
                };
            }

            var idList = await GetReceivedTradeOffersIdList(userId, true);

            if (idList is null)
            {
                return new TradeOffersResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            return new TradeOffersResult
            {
                TradeOffers = idList,
                Success = true
            };
        }

        public async Task<TradeOffersResult> GetSentTradeOffers(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return new TradeOffersResult
                {
                    Errors = new[] { "Invalid user ID" }
                };
            }

            var idList = await GetSentTradeOffersIdList(userId);

            if (idList is null)
            {
                return new TradeOffersResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            return new TradeOffersResult
            {
                TradeOffers = idList,
                Success = true
            };
        }

        public async Task<TradeOffersResult> GetSentRespondedTradeOffers(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return new TradeOffersResult
                {
                    Errors = new[] { "" }
                };
            }

            var idList = await GetSentTradeOffersIdList(userId, true);

            if (idList is null)
            {
                return new TradeOffersResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            return new TradeOffersResult
            {
                TradeOffers = idList,
                Success = true
            };
        }

        public async Task<SentTradeOffer> GetSentTradeOffer(RequestTradeOffer requestTradeOffer)
        {
            if (requestTradeOffer is null || string.IsNullOrEmpty(requestTradeOffer.TradeOfferId) || string.IsNullOrEmpty(requestTradeOffer.UserId))
            {
                return new SentTradeOffer
                {
                    Errors = new[] { "Invalid input data" }
                };
            }

            var entity = await _cacheService.GetCacheValueAsync<CachedTrade>(GetTradesCacheKey(requestTradeOffer.TradeOfferId));
            bool cached = true;

            if (entity is null)
            {
                var response = GetTradeOfferEntity(requestTradeOffer.TradeOfferId);

                if (response is null)
                    return new SentTradeOffer
                    {
                        Errors = new[] { "Something went wrong" }
                    };

                entity = new CachedTrade
                {
                    TradeId = response.TradeId,
                    SenderUserId = await GetSenderId(response.TradeId),
                    ReceiverUserId = await GetReceiverId(response.TradeId),
                    SentDate = response.SentDate,
                    ResponseDate = response.ResponseDate,
                    Response = response.Response
                };

                cached = false;
            }

            if (!Equals(requestTradeOffer.UserId, entity.SenderUserId))
            {
                return new SentTradeOffer
                {
                    Errors = new[] { "User has not sent this trade offer" }
                };
            }

            if (!cached)
            {
                await _cacheService.SetCacheValueAsync(GetTradesCacheKey(requestTradeOffer.TradeOfferId), entity);
            }

            return new SentTradeOffer
            {
                TradeOfferId = requestTradeOffer.TradeOfferId,
                ReceiverId = entity.ReceiverUserId,
                ReceiverName = await GetUsername(entity.ReceiverUserId),
                SentDate = entity.SentDate,
                Items = await GetItemPricesAsync(requestTradeOffer.TradeOfferId),
                Success = true
            };
        }

        public async Task<SentRespondedTradeOffer> GetSentRespondedTradeOffer(RequestTradeOffer requestTradeOffer)
        {
            if (requestTradeOffer is null || string.IsNullOrEmpty(requestTradeOffer.TradeOfferId) || string.IsNullOrEmpty(requestTradeOffer.UserId))
            {
                return new SentRespondedTradeOffer
                {
                    Errors = new[] { "Invalid input data" }
                };
            }

            var entity = await GetSentTradeOffer(requestTradeOffer);

            if (!entity.Success)
            {
                return new SentRespondedTradeOffer
                {
                    Errors = entity.Errors
                };
            }

            var response = await GetTradeResponseAsync(entity.TradeOfferId);
            var responseDate = await GetTradeResponseDateAsync(entity.TradeOfferId);

            if (response is null || responseDate is null)
            {
                return new SentRespondedTradeOffer
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

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
            {
                return new ReceivedTradeOffer
                {
                    Errors = new[] { "Invalid input data" }
                };
            }

            var trade = await _cacheService.GetCacheValueAsync<CachedTrade>(GetTradesCacheKey(requestTradeOffer.TradeOfferId));
            bool cached = true;

            if (trade is null)
            {
                var response = GetTradeOfferEntity(requestTradeOffer.TradeOfferId);

                if (response is not null)
                {
                    trade = new CachedTrade
                    {
                        TradeId = response.TradeId,
                        SenderUserId = await GetSenderId(response.TradeId),
                        ReceiverUserId = await GetReceiverId(response.TradeId),
                        SentDate = response.SentDate,
                        Response = response.Response,
                        ResponseDate = response.ResponseDate,
                        TradeItemsId = (await GetTradeItemsAsync(response.TradeId)).Select(x => x.ItemId).ToList()
                    };

                    cached = false;
                }
            }

            if (!Equals(requestTradeOffer.UserId, trade.ReceiverUserId))
            {
                return new ReceivedTradeOffer
                {
                    Errors = new[] { "User has not sent this trade offer" }
                };
            }

            if (trade is null)
            {
                return new ReceivedTradeOffer
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            if (!cached)
            {
                await _cacheService.SetCacheValueAsync(GetTradesCacheKey(trade.TradeId), trade);
            }

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
            {
                return new ReceivedRespondedTradeOffer
                {
                    Errors = new[] { "Invalid input data" }
                };
            }

            var entity = await GetReceivedTradeOffer(requestTradeOffer);

            if (!entity.Success)
            {
                return new ReceivedRespondedTradeOffer
                {
                    Errors = entity.Errors
                };
            }

            var response = await GetTradeResponseAsync(entity.TradeOfferId);
            var responseDate = await GetTradeResponseDateAsync(entity.TradeOfferId);

            if (response is null || responseDate is null)
            {
                return new ReceivedRespondedTradeOffer
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

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
            string prefix = GetSentTradesCacheKey(userId, "");

            var list = new List<string>();

            var cachedList = await _cacheService.ListWithPrefix<string>(prefix, true);

            if (cachedList.Count == 0)
            {
                var tmp = _context.SentTrades.Where(st => Equals(st.SenderId, userId)).Select(st => st.TradeId).ToList();

                list.AddRange(tmp);

                foreach(var tradeId in tmp)
                {
                    await _cacheService.SetCacheValueAsync(prefix + tradeId, "");
                }
            } else
            {
                list.AddRange(cachedList.Keys);
            }

            await FilterTradeOffers(list, responded);

            return list;
        }

        private async Task<List<string>> GetReceivedTradeOffersIdList(string userId, bool responded = false)
        {
            string prefix = GetReceivedTradesCacheKey(userId, "");

            var list = new List<string>();

            var cachedList = await _cacheService.ListWithPrefix<string>(prefix, true);

            if (cachedList.Count == 0)
            {
                var tmp = _context.ReceivedTrades.Where(o => Equals(userId, o.ReceiverId)).Select(t => t.TradeId).ToList();

                list.AddRange(tmp);

                foreach(var tradeId in tmp)
                {
                    await _cacheService.SetCacheValueAsync(prefix + tradeId, "");
                }
            }
            else
            {
                list.AddRange(cachedList.Keys);
            }

            await FilterTradeOffers(list, responded);

            return list;
        }

        private async Task FilterTradeOffers(List<string> tradeOffersList, bool responded = false)
        {
            int index = 0;

            while (index < tradeOffersList.Count)
            {
                if (await IsResponded(tradeOffersList[index]) != responded)
                {
                    tradeOffersList.RemoveAt(index);
                    continue;
                }

                index++;
            }
        }

        private async Task<bool> IsResponded(string tradeId) => await GetTradeResponseAsync(tradeId) is not null;

        private async Task<List<ItemPrice>> GetItemPricesAsync(string tradeId)
        {
            string prefix = GetTradeItemCacheKey(tradeId, "");

            var items = (await _cacheService.ListWithPrefix<ItemPrice>(prefix)).Values.ToList();

            if (items.Count == 0)
            {
                var tmp = GetTradeContents(tradeId);

                if (tmp != null)
                {
                    foreach (var item in tmp)
                    {
                        var newItem = new ItemPrice
                        {
                            ItemId = item.ItemId,
                            Price = item.Price,
                            Quantity = item.Quantity
                        };

                        newItem.Name = await GetItemNameAsync(item.ItemId);

                        items.Add(newItem);
                        await _cacheService.SetCacheValueAsync(prefix + item.ItemId, newItem);
                    }
                }
            }

            return items;
        }

        private async Task<string> GetReceiverId(string tradeId)
        {
            var cachedTrade = await _cacheService.GetCacheValueAsync<CachedTrade>(GetTradesCacheKey(tradeId));

            if (cachedTrade is null)
            {
                var tmp = GetReceivedTradeOfferEntity(tradeId);

                return tmp?.ReceiverId ?? string.Empty;
            }

            return cachedTrade.ReceiverUserId;
        }

        private async Task<string> GetSenderId(string tradeId)
        {
            var cachedTrade = await _cacheService.GetCacheValueAsync<CachedTrade>(GetTradesCacheKey(tradeId));

            if (cachedTrade is null)
            {
                var tmp = GetSentTradeOfferEntity(tradeId);

                return tmp?.SenderId ?? string.Empty;
            }

            return cachedTrade.SenderUserId;
        }

        /// <returns>null -> trade has no response<br/> 
        /// true -> trade has 'Accepted' as response<br/>
        /// false -> trade has 'Declined' as response</returns>
        private async Task<bool?> GetTradeResponseAsync(string tradeId)
        {
            var cachedTrade = await _cacheService.GetCacheValueAsync<CachedTrade>(GetTradesCacheKey(tradeId));

            if (cachedTrade is null)
            {
                var entity = GetTradeOfferEntity(tradeId);

                return entity?.Response;
            }

            return cachedTrade.Response;
        }

        private async Task<DateTime?> GetTradeResponseDateAsync(string tradeId)
        {
            var cachedTrade = await _cacheService.GetCacheValueAsync<CachedTrade>(GetTradesCacheKey(tradeId));

            if (cachedTrade is null)
            {
                var entity = GetTradeOfferEntity(tradeId);

                return entity?.ResponseDate;
            }

            return cachedTrade.ResponseDate;
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
            {
                await _mediator.Send(new AddItemQuery { UserId = userId, ItemId = item.ItemId, Quantity = item.Quantity });
            }

            return tradeItems.Count != 0;
        }

        // Takes the items from the sender
        private async Task<bool> TakeItemsAsync(string userId, string tradeId)
        {
            var tradeItems = await GetTradeItemsAsync(tradeId);

            foreach (var item in tradeItems)
            {
                await _mediator.Send(new DropItemQuery { UserId = userId, ItemId = item.ItemId, Quantity = item.Quantity });
            }

            return tradeItems.Count != 0;
        }

        private async Task<int> GetTotalPrice(string tradeOfferId)
        {
            var list = await GetItemPricesAsync(tradeOfferId);

            if (list is null || list.Count == 0)
                return 0;

            int total = 0;

            foreach (var item in list)
            {
                total += item.Price;
            }

            return total;
        }

        private List<Entities.TradeContent> GetTradeContents(string tradeId) => _context.TradeContent.Where(t => Equals(t.TradeId, tradeId)).ToList();

        private Entities.SentTrade GetSentTradeOfferEntity(string tradeId) => _context.SentTrades.FirstOrDefault(o => Equals(o.TradeId, tradeId));

        private Entities.ReceivedTrade GetReceivedTradeOfferEntity(string tradeId) => _context.ReceivedTrades.FirstOrDefault(o => Equals(o.TradeId, tradeId));

        private Entities.Trade GetTradeOfferEntity(string tradeId) => _context.Trades.FirstOrDefault(t => Equals(t.TradeId, tradeId));

        private Task<string> GetItemNameAsync(string itemId) => _mediator.Send(new GetItemNameQuery { ItemId = itemId });

        private Task<string> GetUsername(string userId) => _mediator.Send(new GetUsernameQuery { UserId = userId });

        private string GetTradesCacheKey(string tradeId) => CachePrefixKeys.Trades + CachePrefixKeys.Trade + tradeId;

        private string GetSentTradesCacheKey(string userId, string tradeId) => CachePrefixKeys.Trades + CachePrefixKeys.SentTrades + userId + "+" + tradeId;

        private string GetReceivedTradesCacheKey(string userId, string tradeId) => CachePrefixKeys.Trades + CachePrefixKeys.ReceivedTrades + userId + "+" + tradeId;

        private string GetTradeItemCacheKey(string tradeId, string itemId) => CachePrefixKeys.Trades + tradeId + ":" + CachePrefixKeys.TradeItem + itemId;
    }
}
