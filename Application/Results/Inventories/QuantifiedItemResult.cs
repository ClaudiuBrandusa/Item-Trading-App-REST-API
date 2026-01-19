using Application.Results.Items;

namespace Application.Results.Inventories;

public record QuantifiedItemResult : FullItemResult
{
    public int Quantity { get; set; }
}
