using Item_Trading_App_REST_API.Models.Item;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.Item
{
    public interface IItemService
    {
        Task<FullItemResult> CreateItemAsync(CreateItem model);

        Task<FullItemResult> UpdateItemAsync(UpdateItem model);

        Task<DeleteItemResult> DeleteItemAsync(string itemId);

        Task<FullItemResult> GetItemAsync(string itemId);

        IEnumerable<FullItemResult> ListItems();

        Task<string> GetItemNameAsync(string itemId);

        Task<string> GetItemDescriptionAsync(string itemId);
    }
}
