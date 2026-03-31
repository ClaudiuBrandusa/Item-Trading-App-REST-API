using Application.Behaviors.Wallet.GetCash;
using Application.Behaviors.Wallet.GetWallet;
using Application.Behaviors.Wallet.GiveCash;
using Application.Behaviors.Wallet.TakeCash;
using Application.Behaviors.Wallet.UpdateWallet;
using Application.Results.Wallet;
using Domain.Entities.Identity;
using Domain.Repositories;

namespace Application.Services.Wallet;

public class WalletService : IWalletService
{
    private readonly IUserRepository _userRepository;

    public WalletService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<int> GetUserCashAsync(GetUserCashQuery model)
    {
        var user = await GetUser(model.UserId);

        if (user is null)
            return 0;

        return user.Cash;
    }

    public async Task<Result<WalletResult>> GetWalletAsync(GetUserWalletQuery model)
    {
        var user = await GetUser(model.UserId);

        if (user is null)
            return Result<WalletResult>.Failure("User not found");

        return Result<WalletResult>.Success(new WalletResult
        {
            UserId = model.UserId,
            Cash = user.Cash
        });
    }

    public async Task<bool> GiveCashAsync(GiveCashCommand model)
    {
        var user = await GetUser(model.UserId);

        if (user is null)
            return false;

        if (model.Amount < 1)
            return false;

        user.UpdateCashAmount(user.Cash + model.Amount);

        await _userRepository.UpdateUserAsync(user);

        return true;
    }

    public async Task<bool> TakeCashAsync(TakeCashCommand model)
    {
        var user = await GetUser(model.UserId);

        if (user is null)
            return false;

        if (user.Cash - model.Amount < 0)
            return false;

        user.UpdateCashAmount(user.Cash - model.Amount);

        await _userRepository.UpdateUserAsync(user);

        return true;
    }

    public async Task<Result<WalletResult>> UpdateWalletAsync(UpdateWalletCommand model)
    {
        var user = await GetUser(model.UserId);

        if (user is null)
            return Result<WalletResult>.Failure("User not found");

        if (model.Quantity < 0)
            return Result<WalletResult>.Failure("You cannot have a negative balance");

        user.UpdateCashAmount(model.Quantity);

        await _userRepository.UpdateUserAsync(user);

        return Result<WalletResult>.Success(new WalletResult
        {
            UserId = model.UserId,
            Cash = model.Quantity
        });
    }

    private Task<User?> GetUser(string userId) => _userRepository.GetUserByIdAsync(userId);
}
