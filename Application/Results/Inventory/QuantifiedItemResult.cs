using Application.Results.Items;

namespace Application.Results.Inventory;

public record QuantifiedItemResult : FullItemResult
{
    public int Quantity { get; set; }
}
