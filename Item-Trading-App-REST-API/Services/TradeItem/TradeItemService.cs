using Item_Trading_App_REST_API.Constants;
using Item_Trading_App_REST_API.Data;
using Item_Trading_App_REST_API.Entities;
using Item_Trading_App_REST_API.Extensions;
using Item_Trading_App_REST_API.Resources.Commands.TradeItem;
using Item_Trading_App_REST_API.Resources.Events.TradeItem;
using Item_Trading_App_REST_API.Resources.Queries.Item;
using Item_Trading_App_REST_API.Resources.Queries.TradeItem;
using Item_Trading_App_REST_API.Services.Cache;
using Item_Trading_App_REST_API.Services.UnitOfWork;
using MapsterMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Linq;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.TradeItem;

public class TradeItemService : ITradeItemService
{
    private readonly DatabaseContext _context;
    private readonly ICacheService _cacheService;
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public TradeItemService(IDbContextFactory<DatabaseContext> dbContextFactory, ICacheService cacheService, IMediator mediator, IMapper mapper, IUnitOfWorkService unitOfWork)
    {
        _context = dbContextFactory.CreateDbContext();
        _cacheService = cacheService;
        _mediator = mediator;
        _mapper = mapper;

        if (unitOfWork.Transaction is not null)
            _context.Database.UseTransaction(unitOfWork.Transaction.GetDbTransaction());
    }

    public async Task<bool> AddTradeItemAsync(AddTradeItemCommand model)
    {
        if (string.IsNullOrEmpty(model.ItemId) || string.IsNullOrEmpty(model.TradeId) || string.IsNullOrEmpty(model.Name)) return false;

        if (model.Quantity < 1 || model.Price < 1) return false;

        var tradeContent = _mapper.AdaptToType<AddTradeItemCommand, TradeContent>(model);

        var itemNameTask = GetItemNameAsync(model.ItemId);

        await _context.AddAsync(tradeContent);

        await TradeItemCreated(tradeContent, await itemNameTask, model.TradeId);

        return true;
    }

    public async Task<bool> HasTradeItemAsync(HasTradeItemQuery model)
    {
        return await _cacheService.GetEntityAsync(CacheKeys.TradeItem.GetTradeItemKey(model.TradeId, model.ItemId), async (args) =>
        {
            return await _context
                .TradeContent
                .AsNoTracking()
                .Where(x => x.TradeId == model.TradeId && x.ItemId == model.ItemId)
                .FirstOrDefaultAsync();
        }, true) is not default(TradeContent);
    }

    public Task<Models.TradeItems.TradeItem[]> GetTradeItemsAsync(GetTradeItemsQuery model)
    {
        return GetTradeItemsAsync(model.TradeId);
    }

    public Task<string[]> GetItemTradeIdsAsync(GetTradesUsingTheItemQuery model)
    {
        return _cacheService.GetSetValuesAsync(CacheKeys.UsedItem.GetUsedItemKey(model.ItemId), async (args) =>
        {
            return await _context
                .TradeContent
                .AsNoTracking()
                .Where(x => x.ItemId == model.ItemId)
                .Select(x => x.TradeId)
                .ToArrayAsync();
        },
        true);
    }
    public async Task<bool> RemoveTradeItemsAsync(RemoveTradeItemsCommand model)
    {
        var result = await _context
            .TradeContent
            .Where(x => x.TradeId == model.TradeId)
            .ExecuteDeleteAsync();

        if (result == 0) return false;

        await TradeItemRemoved(model.TradeId, model.KeepCache);

        return true;
    }

    private Task<Models.TradeItems.TradeItem[]> GetTradeItemsAsync(string tradeId)
    {
        return _cacheService.GetEntitiesAsync(CacheKeys.TradeItem.GetTradeItemKey(tradeId, ""), async (args) =>
        {
            return await _context
                .TradeContent
                .AsNoTracking()
                .Where(t => Equals(t.TradeId, tradeId))
                .ToArrayAsync();
        }, async (TradeContent content) =>
        {
            return _mapper.AdaptToType<TradeContent, Models.TradeItems.TradeItem>(content, (nameof(Models.TradeItems.TradeItem.Name), await GetItemNameAsync(content.ItemId)));
        },
        true,
        (Models.TradeItems.TradeItem tradeItem) =>
            tradeItem.ItemId
        );
    }

    private Task TradeItemCreated(TradeContent tradeContent, string itemName, string tradeId)
    {
        var tradeItem = _mapper.AdaptToType<TradeContent, Models.TradeItems.TradeItem>(tradeContent, (nameof(Models.TradeItems.TradeItem.Name), itemName));

        var eventNotification = new TradeItemAddedEvent
        {
            TradeId = tradeId,
            Data = tradeItem
        };

        return _mediator.Publish(eventNotification);
    }

    private Task TradeItemRemoved(string tradeId, bool keepCache)
    {
        var eventNotification = new TradeItemRemovedEvent
        {
            TradeId = tradeId,
            KeepCache = keepCache
        };

        return _mediator.Publish(eventNotification);
    }

    private Task<string> GetItemNameAsync(string itemId) => _mediator.Send(new GetItemNameQuery { ItemId = itemId });
}
