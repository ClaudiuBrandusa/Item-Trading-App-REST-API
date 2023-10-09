using Item_Trading_App_REST_API.Constants;
using Item_Trading_App_REST_API.Data;
using Item_Trading_App_REST_API.Models.Item;
using Item_Trading_App_REST_API.Services.Cache;
using Item_Trading_App_REST_API.Services.Notification;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Item_Trading_App_REST_API.Requests.Item;
using Item_Trading_App_REST_API.Requests.Inventory;

namespace Item_Trading_App_REST_API.Services.Item
{
    public class ItemService : IItemService
    {
        private readonly DatabaseContext _context;
        private readonly ICacheService _cacheService;
        private readonly INotificationService _notificationService;
        private readonly IMediator _mediator;

        public ItemService(DatabaseContext context, ICacheService cacheService, INotificationService notificationService, IMediator mediator)
        {
            _context = context;
            _cacheService = cacheService;
            _notificationService = notificationService;
            _mediator = mediator;
        }

        public async Task<FullItemResult> CreateItemAsync(CreateItem model)
        {
            if(model == null)
            {
                return new FullItemResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            var item = new Entities.Item
            {
                ItemId = Guid.NewGuid().ToString(),
                Name = model.ItemName,
                Description = model.ItemDescription
            };

            await _context.Items.AddAsync(item);
            var added = await _context.SaveChangesAsync();

            if (added == 0)
            {
                return new FullItemResult
                {
                    Errors = new[] { "Unable to add this item" }
                };
            }

            await SetItemCacheAsync(item.ItemId, item);
            await _notificationService.SendCreatedNotificationToAllUsersExceptAsync(model.SenderUserId, NotificationCategoryTypes.Item, item.ItemId);

            return new FullItemResult
            {
                ItemId = item.ItemId,
                ItemName = item.Name,
                ItemDescription = item.Description,
                Success = true
            };
        }

        public async Task<FullItemResult> UpdateItemAsync(UpdateItem model)
        {
            if(model == null)
            {
                return new FullItemResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            var item = await GetItemCacheAsync(model.ItemId);

            if (item == null || item == default)
            {
                item = await GetItemEntityAsync(model.ItemId);
                
                if (item == null)
                    return new FullItemResult
                    {
                        Errors = new[] { "Something went wrong" }
                    };
            }

            if(!string.IsNullOrEmpty(model.ItemName) && model.ItemName.Length > 3)
                if(!Equals(item.Name, model.ItemName))
                    item.Name = model.ItemName;
            
            if(!Equals(item.Description, model.ItemDescription))
                item.Description = model.ItemDescription;

            _context.Items.Update(item);
            var updated = await _context.SaveChangesAsync();

            await SetItemCacheAsync(model.ItemId, item);

            if (updated == 0)
            {
                return new FullItemResult
                {
                    ItemId = item.ItemId,
                    ItemName = item.Name,
                    ItemDescription = item.Description,
                    Errors = new[] { "Unable to update item" }
                };
            }

            await _notificationService.SendUpdatedNotificationToAllUsersExceptAsync(model.SenderUserId, NotificationCategoryTypes.Item, item.ItemId);

            return new FullItemResult
            {
                ItemId = item.ItemId,
                ItemName = item.Name,
                ItemDescription = item.Description,
                Success = true
            };
        }

        public async Task<DeleteItemResult> DeleteItemAsync(string itemId, string senderUserId)
        {
            if(string.IsNullOrEmpty(itemId))
            {
                return new DeleteItemResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            var usersOwningTheItem = await _mediator.Send(new GetUserIdsOwningItem { ItemId = itemId });

            var item = await GetItemCacheAsync(itemId);

            if (item == null || item == default)
            {
                item = await GetItemEntityAsync(itemId);

                if (item == null)
                    return new DeleteItemResult
                    { 
                        Errors = new[] { "Something went wrong" }
                    };
            }
            else
            {
                await _cacheService.ClearCacheKeyAsync(CachePrefixKeys.Items + itemId);
            }

            _context.Items.Remove(item);
            var removed = await _context.SaveChangesAsync();

            if(removed == 0)
            {
                return new DeleteItemResult
                {
                    Errors = new[] { "Unable to remove item" }
                };
            }
            
            await _mediator.Send(new ItemDeleted { ItemId = itemId, UserIds = usersOwningTheItem });
            await _notificationService.SendDeletedNotificationToAllUsersExceptAsync(senderUserId, NotificationCategoryTypes.Item, itemId);
            
            return new DeleteItemResult
            {
                ItemId = itemId,
                ItemName = item.Name,
                Success = true
            };
        }

        public async Task<FullItemResult> GetItemAsync(string itemId)
        {
            if(string.IsNullOrEmpty(itemId))
            {
                return new FullItemResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            var item = await GetItemCacheAsync(itemId);

            if (item == null)
            {
                item = await GetItemEntityAsync(itemId, true);

                if (item == null)
                {
                    return new FullItemResult
                    {
                        ItemId = itemId,
                        Errors = new[] { "Item not found" }
                    };
                }

                await SetItemCacheAsync(itemId, item);
            }

            return new FullItemResult
            {
                ItemId = item.ItemId,
                ItemName = item.Name,
                ItemDescription = item.Description,
                Success = true
            };
        }

        public async Task<ItemsResult> ListItems(string searchString = "")
        {
            var items = (await _cacheService.ListWithPrefix<Entities.Item>(CachePrefixKeys.Items)).Values.ToList();

            if (items.Count == 0)
            {
                items = await _context.Items.ToListAsync();

                foreach(var item in items)
                {
                    await SetItemCacheAsync(item.ItemId, item);
                }
            }

            if (!string.IsNullOrEmpty(searchString)) // if it has a search string
                items = items.Where(x => x.Name.ToLower().StartsWith(searchString.ToLower())).ToList();

            return new ItemsResult
            {
                ItemsId = items.Select(i => i.ItemId),
                Success = true
            };
        }

        public async Task<string> GetItemNameAsync(string itemId)
        {
            var entity = await GetItemCacheAsync(itemId);

            if (entity == null)
            {
                entity = await GetItemEntityAsync(itemId, true);

                if (entity == null)
                {
                    return "";
                }

                await SetItemCacheAsync(itemId, entity);
            }

            return entity.Name;
        }

        public async Task<string> GetItemDescriptionAsync(string itemId)
        {
            var entity = await GetItemCacheAsync(itemId);

            if (entity == null)
            {
                entity = await GetItemEntityAsync(itemId, true);

                if (entity == null)
                {
                    return "";
                }

                await SetItemCacheAsync(itemId, entity);
            }

            return entity.Description;
        }

        private async Task<Entities.Item> GetItemEntityAsync(string itemId, bool asNoTracking = false)
        {
            return asNoTracking ?
                await _context.Items.AsNoTracking().FirstOrDefaultAsync(x => x.ItemId == itemId)
                :
                await _context.Items.FirstOrDefaultAsync(x => x.ItemId == itemId);
        }

        private async Task<Entities.Item> GetItemCacheAsync(string itemId) => await _cacheService.GetCacheValueAsync<Entities.Item>(CachePrefixKeys.Items + itemId);
        
        private async Task SetItemCacheAsync(string itemId, Entities.Item entity) => await _cacheService.SetCacheValueAsync(CachePrefixKeys.Items + itemId, entity);
    }
}
