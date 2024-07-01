using Domain.Items;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Trades;

public class TradeContent
{
    public string TradeId { get; set; }

    public string ItemId { get; set; }

    public int Quantity { get; set; }

    public int Price { get; set; }

    [ForeignKey(nameof(TradeId))]
    public Trade Trade { get; set; }

    [ForeignKey(nameof(ItemId))]
    public Item Item { get; set; }
}
