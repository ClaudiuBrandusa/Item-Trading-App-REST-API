namespace Item_Trading_App_REST_API.Models.Item;

public record FullItemResult : ShortItemResult
{
    public string ItemDescription { get; set; }
}
