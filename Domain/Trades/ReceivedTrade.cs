using Domain.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Trades;

public class ReceivedTrade
{
    [Key]
    public string TradeId { get; set; }

    public string ReceiverId { get; set; }

    [ForeignKey(nameof(TradeId))]
    public Trade Trade { get; set; }

    [ForeignKey(nameof(ReceiverId))]
    public User User { get; set; }
}
