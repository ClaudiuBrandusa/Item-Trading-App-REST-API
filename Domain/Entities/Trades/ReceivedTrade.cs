using System.Text.Json.Serialization;
using Domain.Aggregates.Trades;
using Domain.Entities.Identity;
using Domain.Primitives;
namespace Domain.Entities.Trades;

public class ReceivedTrade : Entity
{
    public string TradeId { get; private set; }

    public string ReceiverId { get; private set; }

    [JsonIgnore]
    public virtual Trade? Trade { get; private set; }

    [JsonIgnore]
    public virtual User? User { get; private set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private ReceivedTrade() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public ReceivedTrade(string tradeId, string receiverId)
    {
        TradeId = tradeId;
        ReceiverId = receiverId;
    }

    protected override bool Compare(object obj)
    {
        var entity = obj as ReceivedTrade;

        if (entity is null) return false;

        return entity.TradeId == TradeId &&
               entity.ReceiverId == ReceiverId;
    }

    protected override object GetId() => $"{TradeId}{ReceiverId}";
}
