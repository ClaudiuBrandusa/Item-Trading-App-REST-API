using Application.Models.TradeItems;

namespace Application.Models.Trades;

public record CachedTrade
{
    public string TradeId { get; set; }

    public string SenderUserId { get; set; }

    public string ReceiverUserId { get; set; }

    public bool? Response { get; set; } = null;

    public DateTime SentDate { get; set; }

    public DateTime? ResponseDate { get; set; } = null;

    public TradeItemDTO[] TradeItems { get; set; }

    public CachedTrade(string tradeId, string senderUserId, string receiverUserId, DateTime sentDate, bool? response, DateTime? responseDate, TradeItemDTO[] tradeItems)
    {
        TradeId = tradeId;
        SenderUserId = senderUserId;
        ReceiverUserId = receiverUserId;
        SentDate = sentDate;
        Response = response;
        ResponseDate = responseDate;
        TradeItems = tradeItems;
    }
}
