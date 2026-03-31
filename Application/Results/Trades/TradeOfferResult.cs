using Application.Models.TradeItems;

namespace Application.Results.Trades;

public record TradeOfferResult
{
    public string TradeId { get; set; } = string.Empty;

    public string SenderId { get; set; } = string.Empty;

    public string SenderName { get; set; } = string.Empty;

    public string ReceiverId { get; set; } = string.Empty;

    public string ReceiverName { get; set; } = string.Empty;

    public DateTime CreationDate { get; set; } = DateTime.UtcNow;

    public DateTime? ResponseDate { get; set; }

    public bool? Response { get; set; }

    public IEnumerable<TradeItemDTO> Items { get; set; } = Array.Empty<TradeItemDTO>();
}
