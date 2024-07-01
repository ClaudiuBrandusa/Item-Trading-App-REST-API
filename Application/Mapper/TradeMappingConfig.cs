using Application.Behaviors.Trade.GetTrade;
using Application.Behaviors.Trade.ListTrades;
using Application.Models.Trade;
using Mapster;

namespace Application.Mapper;

public class TradeMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.ForType<string, RequestTradeOfferQuery>()
            .MapWith(str =>
                new RequestTradeOfferQuery { TradeId = str });

        config.ForType<string, ListTradesQuery>()
            .MapWith(str => BuildListTradesQuery(str));
    }

    private static ListTradesQuery BuildListTradesQuery(string userId)
    {
        MapContext.Current!.Parameters.TryGetValue(nameof(ListTradesQuery.Responded), out object responded);
        MapContext.Current!.Parameters.TryGetValue(nameof(ListTradesQuery.TradeDirection), out object tradeDirection);

        return new ListTradesQuery
        {
            UserId = userId,
            TradeItemIds = MapContext.Current!.Parameters[nameof(ListTradesQuery.TradeItemIds)] as string[],
            Responded = responded is not null && bool.Parse(responded.ToString()),
            TradeDirection = tradeDirection is null ? TradeDirection.All : (TradeDirection) tradeDirection
        };
    }
}
