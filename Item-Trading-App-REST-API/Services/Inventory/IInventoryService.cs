using Item_Trading_App_REST_API.Models.Inventory;
using Item_Trading_App_REST_API.Models.Item;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.Inventory
{
    public interface IInventoryService
    {
        bool HasItem(string userId, string itemId, int quantity);

        Task<QuantifiedItemResult> AddItemAsync(string userId, string itemId, int quantity);

        Task<QuantifiedItemResult> DropItemAsync(string userId, string itemId, int quantity);

        Task<QuantifiedItemResult> GetItemAsync(string userId, string itemId);

        Task<InventoryItemsResult> ListItemsAsync(string userId);

        Task<LockItemResult> LockItemAsync(string userId, string itemId, int quantity);

        Task<LockItemResult> UnlockItemAsync(string userId, string itemId, int quantity);
    }
}
