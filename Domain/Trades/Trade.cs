namespace Domain.Trades;

public class Trade
{
    public string TradeId { get; set; }

    public DateTime SentDate { get; set; }

    public DateTime? ResponseDate { get; set; }

    public bool? Response { get; set; }
}
