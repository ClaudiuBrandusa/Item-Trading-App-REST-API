using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Item_Trading_App_REST_API.Entities
{
    public class SentTrade
    {
        [Key]
        public string TradeId { get; set; }

        public string SenderId { get; set; }

        [ForeignKey(nameof(TradeId))]
        public Trade Trade { get; set; }

        [ForeignKey(nameof(SenderId))]
        public User User { get; set; }
    }
}
