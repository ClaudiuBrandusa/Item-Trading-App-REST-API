using Item_Trading_App_REST_API.Entities;
using Item_Trading_App_REST_API.Models.Wallet;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.Wallet;

public class WalletService : IWalletService
{
    private readonly UserManager<User> _userManager;

    public WalletService(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    public async Task<int> GetUserCashAsync(string userId)
    {
        var user = await GetUser(userId);
    
        if (user is null)
            return 0;

        return user.Cash;
    }

    public async Task<WalletResult> GetWalletAsync(string userId)
    {
        var user = await GetUser(userId);

        if (user is null)
            return new WalletResult
            {
                Errors = new[] { "User not found" }
            };

        return new WalletResult
        {
            UserId = userId,
            Cash = user.Cash,
            Success = true
        };
    }

    public async Task<bool> GiveCashAsync(UpdateWallet model)
    {
        var user = await GetUser(model.UserId);

        if (user is null)
            return false;

        if (model.Quantity < 1)
            return false;

        user.Cash += model.Quantity;

        await _userManager.UpdateAsync(user);

        return true;
    }

    public async Task<bool> TakeCashAsync(UpdateWallet model)
    {
        var user = await GetUser(model.UserId);

        if (user is null)
            return false;

        if (user.Cash - model.Quantity < 0)
            return false;

        user.Cash -= model.Quantity;

        await _userManager.UpdateAsync(user);

        return true;
    }

    public async Task<WalletResult> UpdateWalletAsync(UpdateWallet model)
    {
        var user = await GetUser(model.UserId);

        if (user is null)
            return new WalletResult
            {
                Errors = new[] { "User not found" }
            };

        if (model.Quantity < 0)
            return new WalletResult
            {
                Errors = new[] { "You cannot have a negative balance" }
            };

        user.Cash = model.Quantity;

        await _userManager.UpdateAsync(user);

        return new WalletResult
        {
            UserId = model.UserId,
            Cash = model.Quantity,
            Success = true
        };
    }

    private Task<User> GetUser(string userId) => _userManager.FindByIdAsync(userId);
}
