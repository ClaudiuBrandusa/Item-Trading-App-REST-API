using Application.Models.Base;

namespace Application.Models.Wallet;

public record WalletResult : BaseResult
{
    public string UserId { get; set; }

    public int Cash { get; set; }
}
