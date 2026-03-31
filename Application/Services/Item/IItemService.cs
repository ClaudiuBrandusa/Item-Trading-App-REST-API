using Application.Behaviors.Item.CreateItem;
using Application.Behaviors.Item.DeleteItem;
using Application.Behaviors.Item.GetItem;
using Application.Behaviors.Item.GetItemDescription;
using Application.Behaviors.Item.GetItemName;
using Application.Behaviors.Item.ListItems;
using Application.Behaviors.Item.UpdateItem;
using Application.Results.Items;

namespace Application.Services.Item;

public interface IItemService
{
    /// <summary>
    /// Creates a new item
    /// </summary>
    Task<Result<FullItemResult>> CreateItemAsync(CreateItemCommand model);

    /// <summary>
    /// Updates the item
    /// </summary>
    Task<Result<FullItemResult>> UpdateItemAsync(UpdateItemCommand model);

    /// <summary>
    /// Deletes an item
    /// </summary>
    Task<Result<DeleteItemResult>> DeleteItemAsync(DeleteItemCommand model);

    /// <summary>
    /// Returns details about an item
    /// </summary>
    Task<Result<FullItemResult>> GetItemAsync(GetItemQuery model);

    /// <summary>
    /// Enlists all the current items
    /// </summary>
    Task<Result<ItemsResult>> ListItemsAsync(ListItemsQuery model);

    /// <summary>
    /// Returns the item's name
    /// </summary>
    Task<string> GetItemNameAsync(GetItemNameQuery model);

    /// <summary>
    /// Returns the item's description
    /// </summary>
    Task<string> GetItemDescriptionAsync(GetItemDescriptionQuery model);
}
