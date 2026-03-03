using Domain.Entities.Trades;
using Domain.Primitives;

namespace Domain.Aggregates.Trades;

public class Trade : AggregateRoot
{
    public string TradeId { get; private set; }

    public DateTime SentDate { get; private set; }

    public DateTime? ResponseDate { get; private set; }

    public bool? Response { get; private set; }

    private List<TradeItem> _tradeContents = new List<TradeItem>();

    public IReadOnlyCollection<TradeItem> TradeContents => _tradeContents.AsReadOnly();

    public SentTrade SentTrade { get; private set; }

    public ReceivedTrade ReceivedTrade { get; private set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private Trade() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public Trade(DateTime sentDate)
    {
        TradeId = GenerateId();
        SentDate = sentDate;
    }

    public Trade(DateTime sentDate, DateTime? responseDate, bool? response) : this(sentDate)
    {
        ResponseDate = responseDate;
        Response = response;
    }

    public Trade(string tradeId, DateTime sentDate)
    {
        TradeId = tradeId;
        SentDate = sentDate;
    }

    public Trade(string tradeId, DateTime sentDate, DateTime? responseDate, bool? response) : this(tradeId, sentDate)
    {
        ResponseDate = responseDate;
        Response = response;
    }

    public void SetSender(string userId)
    {
        SentTrade = new SentTrade(TradeId, userId);
    }

    public void SetReceiver(string userId)
    {
        ReceivedTrade = new ReceivedTrade(TradeId, userId);
    }

    public void AddTradeContent(TradeItem tradeContent)
    {
        _tradeContents.Add(tradeContent);
    }

    public void RemoveTradeContent(TradeItem tradeContent)
    {
        _tradeContents.Remove(tradeContent);
    }

    public void SetResponse(bool response)
    {
        Response = response;
        ResponseDate = DateTime.Now;
    }

    public static string GenerateId() => Guid.NewGuid().ToString();

    protected override bool Compare(object obj)
    {
        var entity = obj as Trade;

        if (entity is null) return false;

        return entity.TradeId == TradeId &&
               entity.SentDate == SentDate &&
               entity.ResponseDate == ResponseDate &&
               entity.Response == Response;
    }

    protected override object GetId() => TradeId;
}
