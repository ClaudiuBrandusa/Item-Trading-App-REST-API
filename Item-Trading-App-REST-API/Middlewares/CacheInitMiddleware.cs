using Item_Trading_App_REST_API.Services.Item;
using Item_Trading_App_REST_API.Services.TradeItem;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Middlewares;

public class CacheInitMiddleware
{
    private readonly RequestDelegate _next;

    public CacheInitMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context, IItemService itemService, ITradeItemService tradeItemService)
    {
        // using listItems from the item service will set the cache if it is a miss
        var itemsId = (await itemService.ListItemsAsync()).ItemsId.ToList();

        // init used items
        for (int i = 0; i < itemsId.Count; i++)
        {
            await tradeItemService.GetItemTradeIdsAsync(itemsId[i]);
        }

        await _next(context);
    }
}
