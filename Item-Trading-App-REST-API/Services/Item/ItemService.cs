using Item_Trading_App_REST_API.Data;
using Item_Trading_App_REST_API.Models.Item;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.Item
{
    public class ItemService : IItemService
    {
        private readonly DatabaseContext _context;

        public ItemService(DatabaseContext context)
        {
            _context = context;
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

            if(added == 0)
            {
                return new FullItemResult
                {
                    Errors = new[] { "Unable to add this item" }
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

        public async Task<FullItemResult> UpdateItemAsync(UpdateItem model)
        {
            if(model == null)
            {
                return new FullItemResult
                {
                    Errors = new[] { "Something went wrong" }
                };
            }

            var item = _context.Items.FirstOrDefault(i => Equals(i.ItemId, model.ItemId));
        
            if(item == null || item == default)
            {
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

            if(updated == 0)
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

            var item = _context.Items.FirstOrDefault(i => Equals(i.ItemId, itemId));

            if (item == null || item == default)
            {
                return new DeleteItemResult
                { 
                    Errors = new[] { "Something went wrong" }
                };
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

            var item = _context.Items.FirstOrDefault(i => Equals(i.ItemId, itemId));

            if(item == null || item == default)
            {
                return new FullItemResult
                {
                    ItemId = itemId,
                    Errors = new[] { "Item not found" }
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

        public IEnumerable<FullItemResult> ListItems()
        {
            return _context.Items.Select(i => new FullItemResult { ItemId = i.ItemId, ItemName = i.Name, ItemDescription = i.Description, Success = true }).ToList();
        }

        public async Task<string> GetItemNameAsync(string itemId)
        {
            var entity = GetItemEntity(itemId);

            if(entity == null)
            {
                return "";
            }

            return entity.Name;
        }

        public async Task<string> GetItemDescriptionAsync(string itemId)
        {
            var entity = GetItemEntity(itemId);

            if(entity == null)
            {
                return "";
            }

            return entity.Description;
        }

        private Entities.Item GetItemEntity(string itemId) => _context.Items.FirstOrDefault(i => Equals(i.ItemId, itemId));
    }
}
