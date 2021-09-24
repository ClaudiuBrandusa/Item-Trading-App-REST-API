using Item_Trading_App_REST_API.Models.Item;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.Item
{
    public interface IInventoryService
    {

        Task<QuantifiedItemResult> AddItemAsync(string userId, string itemId, int quantity);

        Task<QuantifiedItemResult> DropItemAsync(string userId, string itemId, int quantity);

        Task<QuantifiedItemResult> GetItemAsync(string userId, string itemId);

        Task<InventoryItemsResult> ListItemsAsync(string userId);
    }
}
