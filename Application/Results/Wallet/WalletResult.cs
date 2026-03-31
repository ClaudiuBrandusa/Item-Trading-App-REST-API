namespace Application.Results.Wallet;

public record WalletResult
{
    public string UserId { get; set; } = string.Empty;

    public int Cash { get; set; }
}
