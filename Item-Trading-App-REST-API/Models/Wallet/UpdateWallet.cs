namespace Item_Trading_App_REST_API.Models.Wallet;

public record UpdateWallet
{
    public string UserId { get; set; }

    public int Quantity { get; set; }
}
