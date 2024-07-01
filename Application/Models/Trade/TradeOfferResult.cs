using Application.Models.Base;
using Domain.TradeItems;

namespace Application.Models.Trade;

public record TradeOfferResult : BaseResult
{
    public string TradeId { get; set; }

    public string SenderId { get; set; }

    public string SenderName { get; set; }

    public string ReceiverId { get; set; }

    public string ReceiverName { get; set; }

    public DateTime CreationDate { get; set; } = DateTime.Now;

    public DateTime? ResponseDate { get; set; }

    public bool? Response { get; set; }

    public IEnumerable<TradeItem> Items { get; set; }
}
