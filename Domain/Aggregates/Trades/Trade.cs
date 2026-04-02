using Domain.DomainEvents.Trades;
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

    public IReadOnlyCollection<TradeItem> TradeContents => _tradeContents;

    public SentTrade SentTrade { get; private set; }

    public ReceivedTrade ReceivedTrade { get; private set; }

    public string SenderId { get => GetSenderId(); }

    public string ReceiverId { get => GetReceiverId(); }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private Trade() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public Trade(DateTime sentDate, string senderId, string receiverId)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
        TradeId = GenerateId();
        SentDate = sentDate;

        if (senderId == receiverId)
            throw new ArgumentException("A trade offer must be created between two different users");

        SetSender(senderId);
        SetReceiver(receiverId);

        RaiseDomainEvent(new TradeCreatedDomainEvent(TradeId, receiverId));
    }

    public Trade(DateTime sentDate, string senderId, string receiverId, DateTime? responseDate, bool? response) : this(sentDate, senderId, receiverId)
    {
        ResponseDate = responseDate;
        Response = response;
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public Trade(string tradeId, DateTime sentDate, string senderId, string receiverId)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
        TradeId = tradeId;
        SentDate = sentDate;

        if (senderId == receiverId)
            throw new ArgumentException("A trade offer must be created between two different users");

        SetSender(senderId);
        SetReceiver(receiverId);
    }

    public Trade(string tradeId, DateTime sentDate, DateTime? responseDate, bool? response, string senderId, string receiverId) : this(tradeId, sentDate, senderId, receiverId)
    {
        ResponseDate = responseDate;
        Response = response;
    }

    public void AddTradeContent(TradeItem tradeContent)
    {
        var entity = FindTradeContent(tradeContent.ItemId);
        
        if (entity is null)
        {
            _tradeContents.Add(tradeContent);
            entity = tradeContent;
        }
        else
        {
            entity.Add(tradeContent);
        }

        RaiseDomainEvent(new TradeItemAddedDomainEvent(TradeId, entity.ItemId, entity.Quantity, entity.Price));
    }

    public void RemoveTradeContent(TradeItem tradeContent)
    {
        var entity = FindTradeContent(tradeContent.ItemId);
        
        if (entity is null)
        {
            return;
        }

        _tradeContents.Remove(entity);

        RaiseDomainEvent(new TradeItemRemovedDomainEvent(TradeId, entity.ItemId));
    }

    public void RemoveTradeContent(string itemId)
    {
        var entity = FindTradeContent(itemId);
        
        if (entity is null)
        {
            return;
        }
        
        _tradeContents.Remove(entity);

        RaiseDomainEvent(new TradeItemRemovedDomainEvent(TradeId, entity.ItemId));
    }

    public void ClearTradeContents()
    {
        while (_tradeContents.Count > 0)
        {
            var tradeContent = _tradeContents[0];

            _tradeContents.RemoveAt(0);

            RaiseDomainEvent(new TradeItemRemovedDomainEvent(TradeId, tradeContent.ItemId));
        }
    }

    public TradeItem? GetTradeContent(string itemId) => _tradeContents.Find(x => x.ItemId == itemId);

    public void SetResponse(bool response)
    {
        Response = response;
        ResponseDate = DateTime.UtcNow;
    }

    public string GetSenderId()
    {
        return SentTrade.SenderId;
    }

    public string GetReceiverId()
    {
        return ReceivedTrade.ReceiverId;
    }

    public int GetTotalPrice()
    {
        var total = 0;

        foreach (var tradeContent in TradeContents)
        {
            total += tradeContent.Price;
        }

        return total;
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

    private TradeItem? FindTradeContent(string itemId) => _tradeContents.FirstOrDefault(x => x.ItemId == itemId);

    private void SetSender(string userId)
    {
        SentTrade = new SentTrade(TradeId, userId);
    }

    private void SetReceiver(string userId)
    {
        ReceivedTrade = new ReceivedTrade(TradeId, userId);
    }
}
