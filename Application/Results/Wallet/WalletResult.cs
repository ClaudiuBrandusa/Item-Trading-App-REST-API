using Application.Models;

namespace Application.Results.Wallet;

public record WalletResult : Result
{
    public string UserId { get; set; } = string.Empty;

    public int Cash { get; set; }
}
