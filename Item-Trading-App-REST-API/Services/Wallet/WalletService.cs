using Item_Trading_App_REST_API.Data;
using Item_Trading_App_REST_API.Entities;
using Item_Trading_App_REST_API.Models.Wallet;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
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
