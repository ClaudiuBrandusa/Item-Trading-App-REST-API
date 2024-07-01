using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Trades;

public class TradeContentHistory
{
    public string TradeId { get; set; }

    public string ItemId { get; set; }

    public string ItemName { get; set; }

    public int Quantity { get; set; }

    public int Price { get; set; }

    [ForeignKey(nameof(TradeId))]
    public Trade Trade { get; set; }
}
