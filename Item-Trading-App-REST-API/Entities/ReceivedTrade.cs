using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Item_Trading_App_REST_API.Entities;

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
