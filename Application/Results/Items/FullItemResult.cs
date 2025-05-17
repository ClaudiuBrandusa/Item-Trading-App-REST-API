namespace Application.Results.Items;

public record FullItemResult : ShortItemResult
{
    public string ItemDescription { get; set; } = string.Empty;
}
