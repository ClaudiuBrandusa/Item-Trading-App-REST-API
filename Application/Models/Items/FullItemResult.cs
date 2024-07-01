namespace Application.Models.Items;

public record FullItemResult : ShortItemResult
{
    public string ItemDescription { get; set; }
}
