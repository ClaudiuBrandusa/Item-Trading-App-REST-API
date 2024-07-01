using Domain.Identity;
using System.Security.Claims;

namespace Domain.Repositories;

public interface IIdentityRepository : IUserRepository
{
    /// <summary>
    /// Checks if the <paramref name="password"/> matches with the given <paramref name="user"/>'s password
    /// </summary>
    /// <param name="user"></param>
    /// <param name="password"></param>
    Task<bool> CheckPasswordAsync(User user, string password);

    /// <returns>User's claims</returns>
    Task<IList<Claim>> GetClaimsAsync(User user);
}
