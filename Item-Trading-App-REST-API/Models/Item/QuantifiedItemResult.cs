namespace Item_Trading_App_REST_API.Models.Item;

public record QuantifiedItemResult : FullItemResult
{
    public int Quantity { get; set; }
}
