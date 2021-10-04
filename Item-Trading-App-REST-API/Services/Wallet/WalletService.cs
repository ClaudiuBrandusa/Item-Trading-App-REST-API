using Item_Trading_App_REST_API.Entities;
using Item_Trading_App_REST_API.Models.Wallet;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.Wallet
{
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
        
            if(user == null)
            {
                return 0;
            }

            return user.Cash;
        }

        public async Task<WalletResult> GetWalletAsync(string userId)
        {
            var user = await GetUser(userId);

            if(user == null)
            {
                return new WalletResult
                {
                    Errors = new[] { "User not found" }
                };
            }

            return new WalletResult
            {
                UserId = userId,
                Cash = user.Cash,
                Success = true
            };
        }

        public async Task<bool> GiveCashAsync(string userId, int amount)
        {
            var user = await GetUser(userId);

            if(user == null)
            {
                return false;
            }

            if(amount < 1)
            {
                return false;
            }

            user.Cash += amount;

            await _userManager.UpdateAsync(user);

            return true;
        }

        public async Task<bool> TakeCashAsync(string userId, int amount)
        {
            var user = await GetUser(userId);

            if(user == null)
            {
                return false;
            }

            if(user.Cash - amount < 0)
            {
                return false;
            }

            user.Cash -= amount;

            await _userManager.UpdateAsync(user);

            return true;
        }

        public async Task<WalletResult> UpdateWalletAsync(string userId, int amount)
        {
            var user = await GetUser(userId);

            if(user == null)
            {
                return new WalletResult
                {
                    Errors = new[] { "User not found" }
                };
            }

            if(amount < 0)
            {
                return new WalletResult
                {
                    Errors = new[] { "You cannot have a negative balance" }
                };
            }

            user.Cash = amount;

            await _userManager.UpdateAsync(user);

            return new WalletResult
            {
                UserId = userId,
                Cash = amount,
                Success = true
            };
        }

        private Task<User> GetUser(string userId) => _userManager.FindByIdAsync(userId);
    }
}
