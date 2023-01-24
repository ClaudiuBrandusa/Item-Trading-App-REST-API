using Item_Trading_App_REST_API.Data;
using Item_Trading_App_REST_API.Models.Item;
using Item_Trading_App_REST_API.Models.Trade;
using Item_Trading_App_REST_API.Services.Identity;
using Item_Trading_App_REST_API.Services.Inventory;
using Item_Trading_App_REST_API.Services.Item;
using Item_Trading_App_REST_API.Services.Wallet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.Trade
{
    public class TradeService : ITradeService
    {
        private readonly DatabaseContext _context;
        private readonly IItemService _itemService;
        private readonly IInventoryService _inventoryService;
        private readonly IIdentityService _identityService;
        private readonly IWalletService _walletService;

        public TradeService(DatabaseContext context, IItemService itemService, IInventoryService inventoryService, IIdentityService identityService, IWalletService walletService)
        {
            _context = context;
            _itemService = itemService;
            _inventoryService = inventoryService;
            _identityService = identityService;
            _walletService = walletService;
        }

        public async Task<SentTradeOffer> CreateTradeOffer(CreateTradeOffer model)
        {
            if (model == null || string.IsNullOrEmpty(model.SenderUserId) || string.IsNullOrEmpty(model.TargetUserId) || model.Items == null)
            {
                return new SentTradeOffer
                {
                    Errors = new[] { "Invalid input data" }
                };
            }

            var items = new List<ItemPrice>();

            foreach (var item in model.Items)
            {
                if (item == null)
                    continue;

                if (item.Price < 0)
                    continue;

                if (!_inventoryService.HasItem(model.SenderUserId, item.ItemId, item.Quantity))
                    continue;

                if (!(await _inventoryService.LockItemAsync(model.SenderUserId, item.ItemId, item.Quantity)).Success)
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

            foreach (var item in items)
            {
                var tradeContent = new Entities.TradeContent
                {
                    TradeId = offer.TradeId,
                    ItemId = item.ItemId,
                    Price = item.Price,
                    Quantity = item.Quantity
                };

                _context.TradeContent.Add(tradeContent);
            }

            await _context.SaveChangesAsync();

            _context.SentTrades.Add(new Entities.SentTrade { TradeId = offer.TradeId, SenderId = model.SenderUserId });

            await _context.SaveChangesAsync();

            _context.ReceivedTrades.Add(new Entities.ReceivedTrade { TradeId = offer.TradeId, ReceiverId = model.TargetUserId });

            await _context.SaveChangesAsync();

            return new SentTradeOffer
            {
                TradeOfferId = offer.TradeId,
                ReceiverId = model.TargetUserId,
                ReceiverName = await _identityService.GetUsername(model.TargetUserId),
                Items = items,
                SentDate = offer.SentDate,
                Success = true
            };
        }

        public async Task<AcceptTradeOfferResult> AcceptTradeOffer(string tradeOfferId, string userId)
        {
            if(string.IsNullOrEmpty(tradeOfferId) || string.IsNullOrEmpty(userId))
            {
                return new AcceptTradeOfferResult
                {
                    Errors = new[] { "Invalid IDs" }
                };
            }

            string receiverId = GetReceiverId(tradeOfferId);

            if(!Equals(receiverId, userId))
            {
                return new AcceptTradeOfferResult
                {
                    Errors = new[] { "Invalid userId" }
                };
            }

            var entity = GetTradeOfferEntity(tradeOfferId);

            if(entity == null || entity == default)
            {
                return new AcceptTradeOfferResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            if(entity.Response != null)
            {
                return new AcceptTradeOfferResult
                {
                    Errors = new[] { "Already responded" }
                };
            }

            int price = await GetTotalPrice(tradeOfferId);

            if(price > await _walletService.GetUserCashAsync(userId))
            {
                return new AcceptTradeOfferResult
                {
                    Errors = new[] { "User has not enough money" }
                };
            }

            if(!await _walletService.TakeCashAsync(userId, price))
            {
                return new AcceptTradeOfferResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            string senderId = GetSenderId(tradeOfferId);

            if (!await UnlockTradeItems(senderId, tradeOfferId))
            {
                return new AcceptTradeOfferResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            if(!await GiveItems(userId, tradeOfferId))
            {
                return new AcceptTradeOfferResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }


            if (!await TakeItems(senderId, tradeOfferId))
            {
                return new AcceptTradeOfferResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            if (!await _walletService.GiveCashAsync(senderId, price))
            {

            }

            entity.Response = true;
            entity.ResponseDate = DateTime.Now;

            await _context.SaveChangesAsync();

            return new AcceptTradeOfferResult
            {
                TradeOfferId = tradeOfferId,
                SenderId = senderId,
                SenderName = await _identityService.GetUsername(senderId),
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

            string receiverId = GetReceiverId(tradeOfferId);

            if (!Equals(receiverId, userId))
            {
                return new RejectTradeOfferResult
                {
                    Errors = new[] { "Invalid userId" }
                };
            }

            var entity = GetTradeOfferEntity(tradeOfferId);

            if (entity == null || entity == default)
            {
                return new RejectTradeOfferResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            if (entity.Response != null)
            {
                return new RejectTradeOfferResult
                {
                    Errors = new[] { "Already responded" }
                };
            }

            string senderId = GetSenderId(tradeOfferId);

            if(!await UnlockTradeItems(senderId, tradeOfferId))
            {
                return new RejectTradeOfferResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            entity.Response = false;
            entity.ResponseDate = DateTime.Now;

            await _context.SaveChangesAsync();

            return new RejectTradeOfferResult
            {
                TradeOfferId = tradeOfferId,
                SenderId = senderId,
                SenderName = await _identityService.GetUsername(senderId),
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

            string senderId = GetSenderId(tradeOfferId);

            if (!Equals(senderId, userId))
            {
                return new CancelTradeOfferResult
                {
                    Errors = new[] { "Invalid userId" }
                };
            }

            var entity = GetTradeOfferEntity(tradeOfferId);

            if (entity == null || entity == default)
            {
                return new CancelTradeOfferResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            if(!await UnlockTradeItems(userId, tradeOfferId))
            {
                return new CancelTradeOfferResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            string receiverId = GetReceiverId(tradeOfferId);

            _context.Trades.Remove(entity);
            int removed = await _context.SaveChangesAsync();

            if(removed == 0)
            {
                return new CancelTradeOfferResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            return new CancelTradeOfferResult
            {
                TradeOfferId = tradeOfferId,
                ReceiverId = receiverId,
                ReceiverName = await _identityService.GetUsername(receiverId),
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

            var idList = GetReceivedTradeOffersIdList(userId);

            if (idList == null)
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

            var idList = GetReceivedTradeOffersIdList(userId, true);

            if (idList == null)
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

            var idList = GetSentTradeOffersIdList(userId);

            if (idList == null)
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

            var idList = GetSentTradeOffersIdList(userId, true);

            if (idList == null)
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
            if (requestTradeOffer == null || string.IsNullOrEmpty(requestTradeOffer.TradeOfferId) || string.IsNullOrEmpty(requestTradeOffer.UserId))
            {
                return new SentTradeOffer
                {
                    Errors = new[] { "Invalid input data" }
                };
            }

            var offer = GetTradeOfferEntity(requestTradeOffer.TradeOfferId);

            if (offer == null)
            {
                return new SentTradeOffer
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            if(!IsSender(requestTradeOffer.UserId, requestTradeOffer.TradeOfferId))
            {
                return new SentTradeOffer
                {
                    Errors = new[] { "User has not sent this trade offer" }
                };
            }

            string receiverId = GetReceiverId(requestTradeOffer.TradeOfferId);

            return new SentTradeOffer
            {
                TradeOfferId = requestTradeOffer.TradeOfferId,
                ReceiverId = receiverId,
                ReceiverName = await _identityService.GetUsername(receiverId),
                SentDate = offer.SentDate,
                Items = await GetItemPrices(requestTradeOffer.TradeOfferId),
                Success = true
            };
        }

        public async Task<SentRespondedTradeOffer> GetSentRespondedTradeOffer(RequestTradeOffer requestTradeOffer)
        {
            if (requestTradeOffer == null || string.IsNullOrEmpty(requestTradeOffer.TradeOfferId) || string.IsNullOrEmpty(requestTradeOffer.UserId))
            {
                return new SentRespondedTradeOffer
                {
                    Errors = new[] { "Invalid input data" }
                };
            }

            var offer = await GetSentTradeOffer(requestTradeOffer);

            if(!offer.Success)
            {
                return new SentRespondedTradeOffer
                {
                    Errors = offer.Errors
                };
            }

            var response = GetTradeOfferEntity(requestTradeOffer.TradeOfferId);

            if(response == null || response.Response == null || response.ResponseDate == null)
            {
                return new SentRespondedTradeOffer
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            return new SentRespondedTradeOffer
            {
                TradeOfferId = requestTradeOffer.TradeOfferId,
                ReceiverId = offer.ReceiverId,
                ReceiverName = offer.ReceiverName,
                Response = (bool)response.Response,
                ResponseDate = (DateTime)response.ResponseDate,
                SentDate = response.SentDate,
                Items = offer.Items,
                Success = true
            };
        }

        public async Task<ReceivedTradeOffer> GetReceivedTradeOffer(RequestTradeOffer requestTradeOffer)
        {
            if (requestTradeOffer == null || string.IsNullOrEmpty(requestTradeOffer.TradeOfferId) || string.IsNullOrEmpty(requestTradeOffer.UserId))
            {
                return new ReceivedTradeOffer
                {
                    Errors = new[] { "Invalid input data" }
                };
            }

            var offer = GetReceivedTradeOfferEntity(requestTradeOffer.TradeOfferId);
        
            if(offer == null)
            {
                return new ReceivedTradeOffer
                { 
                    Errors = new[] { "Something went wrong" }
                };
            }

            if (!IsReceiver(requestTradeOffer.UserId, requestTradeOffer.TradeOfferId))
            {
                return new ReceivedTradeOffer
                {
                    Errors = new[] { "User has not sent this trade offer" }
                };
            }

            string senderId = GetSenderId(requestTradeOffer.TradeOfferId);

            var trade = GetTradeOfferEntity(requestTradeOffer.TradeOfferId);

            if(trade == null)
            {
                return new ReceivedTradeOffer
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            return new ReceivedTradeOffer
            {
                TradeOfferId = requestTradeOffer.TradeOfferId,
                SenderId = senderId,
                SenderName = await _identityService.GetUsername(senderId),
                SentDate = trade.SentDate,
                Items = await GetItemPrices(requestTradeOffer.TradeOfferId),
                Success = true
            };
        }

        public async Task<ReceivedRespondedTradeOffer> GetReceivedRespondedTradeOffer(RequestTradeOffer requestTradeOffer)
        {
            if (requestTradeOffer == null || string.IsNullOrEmpty(requestTradeOffer.TradeOfferId) || string.IsNullOrEmpty(requestTradeOffer.UserId))
            {
                return new ReceivedRespondedTradeOffer
                {
                    Errors = new[] { "Invalid input data" }
                };
            }

            var offer = await GetReceivedTradeOffer(requestTradeOffer);

            if(!offer.Success)
            {
                return new ReceivedRespondedTradeOffer
                {
                    Errors = offer.Errors
                };
            }

            var trade = GetTradeOfferEntity(requestTradeOffer.TradeOfferId);

            if(trade == null || trade.Response == null || trade.ResponseDate == null)
            {
                return new ReceivedRespondedTradeOffer
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            string senderId = GetSenderId(requestTradeOffer.TradeOfferId);

            return new ReceivedRespondedTradeOffer
            {
                TradeOfferId = requestTradeOffer.TradeOfferId,
                SenderId = senderId,
                SenderName = await _identityService.GetUsername(senderId),
                Response = (bool)trade.Response,
                ResponseDate = (DateTime)trade.ResponseDate,
                SentDate = offer.SentDate,
                Items = await GetItemPrices(requestTradeOffer.TradeOfferId),
                Success = true
            };
        }

        private List<string> GetSentTradeOffersIdList(string userId, bool responded = false)
        {
            var list = _context.SentTrades.Where(st => Equals(st.SenderId, userId)).Select(st => st.TradeId).ToList();

            if (list == null)
                return new List<string>();

            int index = 0;

            while (index < list.Count)
            {
                if (IsResponded(list[index]) != responded)
                {
                    list.RemoveAt(index);
                    continue;
                }

                index++;
            }

            return list;
        }

        private List<string> GetReceivedTradeOffersIdList(string receiverId, bool responded = false)
        {
            var list = _context.ReceivedTrades.Where(o => Equals(receiverId, o.ReceiverId)).Select(t => t.TradeId).ToList();
        
            if(list == null || list.Count == 0)
            {
                return new List<string>();
            }

            int index = 0;

            while(index < list.Count)
            {
                if(IsResponded(list[index]) != responded)
                {
                    list.RemoveAt(index);
                    continue;
                }   

                index++;
            }

            return list;
        }

        private bool IsResponded(string tradeId)
        {
            if(string.IsNullOrEmpty(tradeId))
            {
                return false;
            }

            var trade = GetTradeOfferEntity(tradeId);

            if(trade == null)
            {
                return false;
            }

            return trade.Response != null;
        }

        private async Task<List<ItemPrice>> GetItemPrices(string tradeId)
        {
            var tmp = GetTradeContents(tradeId);

            var items = new List<ItemPrice>();

            if (tmp != null && tmp != default)
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
                }
            }

            return items;
        }

        private string GetReceiverId(string tradeId)
        {
            var tmp = GetReceivedTradeOfferEntity(tradeId);

            if (tmp == null)
                return "";

            return tmp.ReceiverId;
        }

        private string GetSenderId(string tradeId)
        {
            var tmp = GetSentTradeOfferEntity(tradeId);

            if (tmp == null)
                return "";

            return tmp.SenderId;
        }

        private async Task<List<TradeItem>> GetTradeItems(string tradeId)
        {
            var tradeContents = await GetItemPrices(tradeId);

            var results = new List<TradeItem>();

            if (tradeContents != null && tradeContents.Count > 0)
            {
                foreach(var content in tradeContents)
                {
                    if (content == null)
                        continue;

                    results.Add(new TradeItem
                    {
                        ItemId = content.ItemId,
                        Quantity = content.Quantity
                    });
                }
            }

            return results;
        }

        private async Task<bool> UnlockTradeItems(string userId, string tradeId)
        {
            var tradeItems = await GetTradeItems(tradeId);

            if(tradeItems == null || tradeItems.Count == 0)
            {
                return false;
            }

            foreach(var item in tradeItems)
            {
                if (item == null)
                    continue;

                await _inventoryService.UnlockItemAsync(userId, item.ItemId, item.Quantity);
            }

            return true;
        }

        // Takes the items from trade to the receiver
        private async Task<bool> GiveItems(string userId, string tradeId)
        {
            var tradeItems = GetTradeContents(tradeId);

            if(tradeItems == null || tradeItems.Count == 0)
            {
                return false;
            }

            foreach(var item in tradeItems)
            {
                await _inventoryService.AddItemAsync(userId, item.ItemId, item.Quantity);
            }

            return true;
        }

        // Takes the items from the sender
        private async Task<bool> TakeItems(string userId, string tradeId)
        {
            var tradeItems = GetTradeContents(tradeId);

            if(tradeItems == null || tradeItems.Count == 0)
            {
                return false;
            }

            foreach(var item in tradeItems)
            {
                await _inventoryService.DropItemAsync(userId, item.ItemId, item.Quantity);
            }

            return true;
        }

        private async Task<int> GetTotalPrice(string tradeOfferId)
        {
            var list = await GetItemPrices(tradeOfferId);

            if (list == null || list.Count == 0)
                return 0;

            int total = 0;

            foreach(var item in list)
            {
                total += item.Price;
            }

            return total;
        }

        private bool IsSender(string userId, string tradeOfferId) => Equals(GetSenderId(tradeOfferId), userId);

        private bool IsReceiver(string userId, string tradeOfferId) => Equals(GetReceiverId(tradeOfferId), userId);

        private List<Entities.TradeContent> GetTradeContents(string tradeId) => _context.TradeContent.Where(t => Equals(t.TradeId, tradeId)).ToList();
    
        private Entities.SentTrade GetSentTradeOfferEntity(string tradeId) => _context.SentTrades.FirstOrDefault(o => Equals(o.TradeId, tradeId));

        private Entities.ReceivedTrade GetReceivedTradeOfferEntity(string tradeId) => _context.ReceivedTrades.FirstOrDefault(o => Equals(o.TradeId, tradeId));

        private Entities.Trade GetTradeOfferEntity(string tradeId) => _context.Trades.FirstOrDefault(t => Equals(t.TradeId, tradeId));

        private Task<string> GetItemNameAsync(string itemId) => _itemService.GetItemNameAsync(itemId);
    }
}
