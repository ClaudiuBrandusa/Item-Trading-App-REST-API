using Domain.Entities.Identity;
using Domain.Repositories.Identity;
using Infrastructure.Data;
using Infrastructure.Services.DatabaseContextWrapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Infrastructure.Repositories.Identity;
public class IdentityRepository : RepositoryBase, IIdentityRepository
{
    private readonly UserManager<User> _userManager;

    public IdentityRepository(DatabaseContext context, IDatabaseContextWrapper databaseContextWrapper, UserManager<User> userManager) : base(context)
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
        var user = await _context.Users.FindAsync(userId);

        if (user is null)
            return "";

        return user.UserName ?? string.Empty;
    }

    public async Task<Dictionary<string, string>> GetUsernamesAsync(string[] userIds)
    {
        var rows = await _context.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.UserName })
            .ToListAsync();

        return rows.ToDictionary(u => u.Id, u => u.UserName!);
    }

    public async Task<IdentityResult> CreateUserAsync(User user, string password)
    {
        var createdUser = await _userManager.CreateAsync(user, password);
        await _context.SaveChangesAsync();
        return createdUser;
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
        return _context.RefreshTokens.SingleOrDefaultAsync(x => x.Token == refreshTokenId);
    }

    public Task<List<string>> ListUsersAsync(string searchString)
    {
        return string.IsNullOrEmpty(searchString) ?
            _context.Users
                .AsNoTracking()
                .Select(u => u.Id)
                .ToListAsync()
                :
            _context.Users
                .AsNoTracking()
                .Where(u => u.UserName!.StartsWith(searchString))
                .Select(u => u.Id)
                .ToListAsync();
    }
}
