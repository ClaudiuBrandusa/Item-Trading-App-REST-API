using Application.Models.RefreshToken;
using Application.Services.UnitOfWork;
using Domain.Repositories;
using Item_Trading_App_REST_API.Options;

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

    public async Task<RefreshTokenResult> GenerateRefreshTokenAsync(string userId, string jti)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(jti))
            return new RefreshTokenResult { Errors = new[] { "Invalid input data" } };

        var user = await _repository.GetUserAsync(userId);

        if (user is null)
            return new RefreshTokenResult { Errors = new[] { "User not found" } };

        var refreshToken = await _repository.GenerateRefreshTokenAsync(user.Id, jti, _jwtSettings.RefreshTokenLifetime);
        
        return new RefreshTokenResult
        {
            Success = true,
            Token = refreshToken.Token,
            UserId = refreshToken.UserId,
            Used = refreshToken.Used,
            CreationDate = refreshToken.CreationDate,
            ExpiryDate = refreshToken.ExpiryDate,
            Invalidated = refreshToken.Invalidated
        };
    }

    public async Task<RefreshTokenResult> GetRefreshTokenAsync(string refreshTokenId)
    {
        var refreshToken = await _repository.GetRefreshTokenAsync(refreshTokenId);

        if (refreshToken is null || refreshToken == default)
            return null;

        return new RefreshTokenResult
        {
            Token = refreshToken.Token,
            UserId = refreshToken.UserId,
            CreationDate = refreshToken.CreationDate,
            ExpiryDate = refreshToken.ExpiryDate,
            Invalidated = refreshToken.Invalidated,
            Used = refreshToken.Used,
            Success = true
        };
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

    public async Task<RefreshTokenResult> GetRecentRefreshTokenAsync(string userId, string jti)
    {
        var lastRefreshToken = await _repository.GetLastRefreshTokenAsync(userId, jti);

        if (lastRefreshToken is null)
        {
            return new RefreshTokenResult
            {
                Errors = new[] { "No refresh token found" }
            };
        }

        return new RefreshTokenResult
        {
            Success = true,
            Token = lastRefreshToken.Token,
            UserId = userId,
            CreationDate = lastRefreshToken.CreationDate,
            ExpiryDate = lastRefreshToken.ExpiryDate,
            Used = lastRefreshToken.Used,
            Invalidated = lastRefreshToken.Invalidated
        };
    }

    private async Task ClearExpiredRefreshTokens()
    {
        _unitOfWorkService.BeginTransaction();

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
            _unitOfWorkService.CommitTransaction();
        }
        catch (Exception)
        {
            _unitOfWorkService.RollbackTransaction();
            throw;
        }
    }

    private async Task ClearUsedRefreshTokens()
    {
        _unitOfWorkService.BeginTransaction();

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
            _unitOfWorkService.CommitTransaction();
        }
        catch (Exception)
        {
            _unitOfWorkService.RollbackTransaction();
            throw;
        }
    }

    private async Task ClearOldRefreshTokens()
    {
        _unitOfWorkService.BeginTransaction();

        try
        {
            var usersId = await _repository.ListUserIdsAsync();

            if (usersId is null)
                return;

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

            _unitOfWorkService.CommitTransaction();
        }
        catch (Exception)
        {
            _unitOfWorkService.RollbackTransaction();
            throw;
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _repository.Dispose();
    }
}
