using Item_Trading_App_REST_API.Constants;
using Item_Trading_App_REST_API.Data;
using Item_Trading_App_REST_API.Models.Item;
using Item_Trading_App_REST_API.Services.Cache;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.Item
{
    public class ItemService : IItemService
    {
        private readonly DatabaseContext _context;
        private readonly ICacheService _cacheService;

        public ItemService(DatabaseContext context, ICacheService cacheService)
        {
            _context = context;
            _cacheService = cacheService;
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

            await SetItemCache(item.ItemId, item);

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

            var item = await GetItemCache(model.ItemId);

            if (item == null || item == default)
            {
                item = _context.Items.FirstOrDefault(i => Equals(i.ItemId, model.ItemId));
                
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

            await SetItemCache(model.ItemId, item);

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

            return new FullItemResult
            {
                ItemId = item.ItemId,
                ItemName = item.Name,
                ItemDescription = item.Description,
                Success = true
            };
        }

        public async Task<DeleteItemResult> DeleteItemAsync(string itemId)
        {
            if(string.IsNullOrEmpty(itemId))
            {
                return new DeleteItemResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            var item = await GetItemCache(itemId);

            if (item == null || item == default)
            {
                item = _context.Items.FirstOrDefault(i => Equals(i.ItemId, itemId));

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

            var item = await GetItemCache(itemId);

            if (item == null)
            {
                item = _context.Items.FirstOrDefault(i => Equals(i.ItemId, itemId));

                if (item == null)
                {
                    return new FullItemResult
                    {
                        ItemId = itemId,
                        Errors = new[] { "Item not found" }
                    };
                }

                await SetItemCache(itemId, item);
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
                items = _context.Items.ToList();

                await Parallel.ForEachAsync(items, async (item, cancellationToken) => await SetItemCache(item.ItemId, item));
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
            var entity = await GetItemCache(itemId);

            if (entity == null)
            {
                entity = GetItemEntity(itemId);

                if (entity == null)
                {
                    return "";
                }

                await SetItemCache(itemId, entity);
            }

            return entity.Name;
        }

        public async Task<string> GetItemDescriptionAsync(string itemId)
        {
            var entity = await GetItemCache(itemId);

            if (entity == null)
            {
                entity = GetItemEntity(itemId);

                if (entity == null)
                {
                    return "";
                }

                await SetItemCache(itemId, entity);
            }

            return entity.Description;
        }

        private Entities.Item GetItemEntity(string itemId) => _context.Items.FirstOrDefault(i => Equals(i.ItemId, itemId));

        private async Task<Entities.Item> GetItemCache(string itemId) => await _cacheService.GetCacheValueAsync<Entities.Item>(CachePrefixKeys.Items + itemId);
        
        private async Task SetItemCache(string itemId, Entities.Item entity) => await _cacheService.SetCacheValueAsync(CachePrefixKeys.Items + itemId, entity);
    }
}
