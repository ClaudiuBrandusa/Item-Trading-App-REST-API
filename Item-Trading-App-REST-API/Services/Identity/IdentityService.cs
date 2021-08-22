using Item_Trading_App_REST_API.Data;
using Item_Trading_App_REST_API.Models;
using Item_Trading_App_REST_API.Options;
using Microsoft.AspNetCore.Identity;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Services.Identity
{
    public class IdentityService : IIdentityService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly JwtSettings _jwtSettings;
        private readonly DatabaseContext _context;

        public IdentityService(UserManager<IdentityUser> userManager, JwtSettings jwtSettings, DatabaseContext context)
        {
            _userManager = userManager;
            _jwtSettings = jwtSettings;
            _context = context;
        }

        public async Task<AuthenticationResult> RegisterAsync(string username, string password)
        {
            var user = await _userManager.FindByNameAsync(username);

            if(user != null)
            {
                return new AuthenticationResult
                { 
                    Errors = new[] { "User with this username already exists" }
                };
            }

            var newUserId = Guid.NewGuid();

            var newUser = new IdentityUser
            {
                Id = newUserId.ToString(),
                UserName = username
            };

            var createdUser = await _userManager.CreateAsync(newUser, password);

            if(!createdUser.Succeeded)
            {
                return new AuthenticationResult
                { 
                    Errors = createdUser.Errors.Select(x => x.Description)
                };
            }

            return await GetAuthenticationResultForUser(newUser);
        }

        public async Task<AuthenticationResult> LoginAsync(string username, string password)
        {
            var user = await _userManager.FindByNameAsync(username);

            if(user == null)
            {
                return new AuthenticationResult
                { 
                    Errors = new[] { "User does not exist" }
                };
            }

            var userMatchPassword = await _userManager.CheckPasswordAsync(user, password);

            if(!userMatchPassword)
            {
                return new AuthenticationResult
                { 
                    Errors = new[] { "Username or password is wrong" }
                };
            }

            return await GetAuthenticationResultForUser(user);
        }
        private async Task<AuthenticationResult> GetAuthenticationResultForUser(IdentityUser newUser)
        {
            return new AuthenticationResult
            {
                Success = true
            };
        }
    }
}
