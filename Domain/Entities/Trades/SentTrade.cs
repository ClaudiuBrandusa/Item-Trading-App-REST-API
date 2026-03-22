using System.Text.Json.Serialization;
using Domain.Aggregates.Trades;
using Domain.Entities.Identity;
using Domain.Primitives;

namespace Domain.Entities.Trades;

public class SentTrade : Entity
{
    public string TradeId { get; private set; }

    public string SenderId { get; private set; }

    [JsonIgnore]
    public virtual Trade Trade { get; private set; }

    [JsonIgnore]
    public virtual User User { get; private set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private SentTrade() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public SentTrade(string tradeId, string senderId)
    {
        TradeId = tradeId;
        SenderId = senderId;
    }

    protected override bool Compare(object obj)
    {
        if (obj is not SentTrade entity) return false;

        return entity.TradeId == TradeId &&
               entity.SenderId == SenderId;
    }

    protected override object GetId() => $"{TradeId}{SenderId}";
}
