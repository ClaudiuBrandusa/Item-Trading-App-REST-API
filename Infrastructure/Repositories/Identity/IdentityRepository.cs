using Domain.Entities.Identity;
using Domain.Repositories;
using Infrastructure.Services.DatabaseContextWrapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Infrastructure.Repositories.Identity;
public class IdentityRepository : RepositoryBase, IIdentityRepository
{
    private readonly UserManager<User> _userManager;

    public IdentityRepository(IDatabaseContextWrapper databaseContextWrapper, UserManager<User> userManager) : base(databaseContextWrapper)
    {
        _userManager = userManager;
    }

    public Task<User?> GetUserByIdAsync(string userId)
    {
        return _userManager.FindByIdAsync(userId);
    }

    public Task<User?> GetUserByNameAsync(string username)
    {
        return _userManager.FindByNameAsync(username);
    }

    public async Task<string> GetUsernameAsync(string userId)
    {
        var dbContext = await DatabaseContextWrapper.ProvideDatabaseContextAsync();

        var user = await dbContext.Users.FindAsync(userId);

        if (user is null)
            return "";

        DatabaseContextWrapper.DisposeDatabaseContext(dbContext);

        return user.UserName ?? string.Empty;
    }

    public Task<IdentityResult> CreateUserAsync(User user, string password)
    {
        return _userManager.CreateAsync(user, password);
    }

    public async Task<bool> UpdateUserAsync(User user)
    {
        return (await _userManager.UpdateAsync(user)).Succeeded;
    }

    public Task<bool> CheckPasswordAsync(User user, string password)
    {
        return _userManager.CheckPasswordAsync(user, password);
    }

    public Task<IList<Claim>> GetClaimsAsync(User user)
    {
        return _userManager.GetClaimsAsync(user);
    }

    public Task<RefreshToken?> GetRefreshTokenAsync(string refreshTokenId)
    {
        return context.RefreshTokens.SingleOrDefaultAsync(x => x.Token == refreshTokenId);
    }

    public Task<List<string>> ListUsersAsync(string searchString)
    {
        return string.IsNullOrEmpty(searchString) ?
            context.Users
                .AsNoTracking()
                .Select(u => u.Id)
                .ToListAsync()
                :
            context.Users
                .AsNoTracking()
                .Where(u => u.UserName!.StartsWith(searchString))
                .Select(u => u.Id)
                .ToListAsync();
    }
}
