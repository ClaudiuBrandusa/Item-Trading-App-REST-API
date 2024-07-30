using Domain.Aggregates.Trades;
using Domain.Entities.Trades;

namespace Domain.ValueObjects.Trades;

public record CachedTrade
{
    public string TradeId { get; set; }

    public string SenderUserId { get; set; }

    public string ReceiverUserId { get; set; }

    public bool? Response { get; set; } = null;

    public DateTime SentDate { get; set; }

    public DateTime? ResponseDate { get; set; } = null;

    public TradeItem[] TradeItems { get; set; }

    public CachedTrade(string tradeId, string senderUserId, string receiverUserId, DateTime sentDate, bool? response, DateTime? responseDate, TradeItem[] tradeItems)
    {
        TradeId = tradeId;
        SenderUserId = senderUserId;
        ReceiverUserId = receiverUserId;
        SentDate = sentDate;
        Response = response;
        ResponseDate = responseDate;
        TradeItems = tradeItems;
    }

    public bool IsPartOfTrade(Trade trade)
    {
        if (trade is null) return false;

        return trade.TradeId == TradeId &&
               trade.SentDate == SentDate &&
               trade.ResponseDate == ResponseDate &&
               trade.Response == Response;
    }
}
