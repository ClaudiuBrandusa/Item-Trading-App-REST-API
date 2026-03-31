using Application.Results.RefreshToken;
using Application.Services.UnitOfWork;
using Domain.Repositories.Identity;
using Application.Options;

namespace Application.Services.RefreshToken;

public class RefreshTokenService : IRefreshTokenService, IDisposable
{
    private readonly IRefreshTokenRepository _repository;
    private readonly IUnitOfWorkService _unitOfWorkService;
    private readonly JwtSettings _jwtSettings;
    
    public RefreshTokenService(IRefreshTokenRepository repository, IUnitOfWorkService unitOfWorkService, JwtSettings jwtSettings)
    {
        _repository = repository;
        _unitOfWorkService = unitOfWorkService;
        _jwtSettings = jwtSettings;
    }

    public async Task<Result<RefreshTokenResult>> GenerateRefreshTokenAsync(string userId, string jti)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(jti))
            return Result<RefreshTokenResult>.Failure("Invalid input data");

        var user = await _repository.GetUserAsync(userId);

        if (user is null)
            return Result<RefreshTokenResult>.Failure("User not found");

        var refreshToken = await _repository.GenerateRefreshTokenAsync(user.Id, jti, _jwtSettings.RefreshTokenLifetime);
        
        return Result<RefreshTokenResult>.Success(new RefreshTokenResult
        {
            Token = refreshToken.Token,
            UserId = refreshToken.UserId,
            Used = refreshToken.Used,
            CreationDate = refreshToken.CreationDate,
            ExpiryDate = refreshToken.ExpiryDate,
            JwtId = refreshToken.JwtId,
            Invalidated = refreshToken.Invalidated
        });
    }

    public async Task<Result<RefreshTokenResult>> GetRefreshTokenAsync(string refreshTokenId)
    {
        var refreshToken = await _repository.GetRefreshTokenAsync(refreshTokenId);

        if (refreshToken is null)
            return Result<RefreshTokenResult>.Failure("Something went wrong");

        return Result<RefreshTokenResult>.Success(new RefreshTokenResult
        {
            Token = refreshToken.Token,
            UserId = refreshToken.UserId,
            CreationDate = refreshToken.CreationDate,
            ExpiryDate = refreshToken.ExpiryDate,
            Invalidated = refreshToken.Invalidated,
            Used = refreshToken.Used,
            JwtId = refreshToken.JwtId
        });
    }

    public Task<bool> RemoveRefreshTokenAsync(string refreshTokenId)
    {
        return _repository.DeleteRefreshTokenAsync(refreshTokenId);
    }

    public async Task ClearRefreshTokensAsync()
    {
        await ClearExpiredRefreshTokens();
        await ClearUsedRefreshTokens();
        await ClearOldRefreshTokens();
    }

    public async Task<Result<RefreshTokenResult>> GetRecentRefreshTokenAsync(string userId, string jti)
    {
        var lastRefreshToken = await _repository.GetLastRefreshTokenAsync(userId, jti);

        if (lastRefreshToken is null)
        {
            return Result<RefreshTokenResult>.Failure("No refresh token found");
        }

        return Result<RefreshTokenResult>.Success(new RefreshTokenResult
        {
            Token = lastRefreshToken.Token,
            UserId = userId,
            CreationDate = lastRefreshToken.CreationDate,
            ExpiryDate = lastRefreshToken.ExpiryDate,
            Used = lastRefreshToken.Used,
            JwtId = lastRefreshToken.JwtId,
            Invalidated = lastRefreshToken.Invalidated
        });
    }

    private async Task ClearExpiredRefreshTokens()
    {
        await _unitOfWorkService.ExplicitTransaction(async () =>
        {
            try
            {
                var expiredTokens = await _repository.ListExpiredRefreshTokenIdsAsync();

                if (expiredTokens is not null)
                {
                    foreach (var token in expiredTokens)
                    {
                        await _repository.DeleteRefreshTokenAsync(token);
                    }
                }

                await _repository.SaveChangesAsync();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception in {nameof(RefreshTokenService)}.{nameof(ClearExpiredRefreshTokens)}: {e.Message}");
                return false;
            }
        });
    }

    private async Task ClearUsedRefreshTokens()
    {
        await _unitOfWorkService.ExplicitTransaction(async () =>
        {
            try
            {
                var usedTokens = await _repository.ListUsedRefreshTokenIdsAsync();

                if (usedTokens is not null)
                {
                    foreach (var tokenId in usedTokens)
                    {
                        await _repository.DeleteRefreshTokenAsync(tokenId);
                    }
                }

                await _repository.SaveChangesAsync();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception in {nameof(RefreshTokenService)}.{nameof(ClearUsedRefreshTokens)}: {e.Message}");
                return false;
            }
        });
    }

    private async Task ClearOldRefreshTokens()
    {
        await _unitOfWorkService.ExplicitTransaction(async () =>
        {
            try
            {
                var usersId = await _repository.ListUserIdsAsync();

                if (usersId is null)
                    return false;

                foreach (string userId in usersId)
                {
                    var tokens = await _repository.ListRefreshTokensAsync(userId);

                    if (tokens is null)
                        continue;

                    if (tokens.Length <= _jwtSettings.AllowedRefreshTokensPerUser)
                        continue;

                    tokens = tokens.OrderBy(x => x.CreationDate.Ticks).ToArray();

                    int n = tokens.Length - _jwtSettings.AllowedRefreshTokensPerUser; // number of tokens to be deleted

                    if (n < 1)
                        continue;

                    for (int i = 0; i < n; i++)
                        await _repository.DeleteRefreshTokenAsync(tokens[i]);

                    await _repository.SaveChangesAsync();
                }

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception in {nameof(RefreshTokenService)}.{nameof(ClearOldRefreshTokens)}: {e.Message}");
                return false;
            }
        });
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _repository.Dispose();
    }
}
