using Application.Models.Items;

namespace Application.Models.Inventory;

public record QuantifiedItemResult : FullItemResult
{
    public int Quantity { get; set; }
}
