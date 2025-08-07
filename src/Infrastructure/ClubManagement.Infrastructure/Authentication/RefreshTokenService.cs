using ClubManagement.Domain.Entities;
using ClubManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

namespace ClubManagement.Infrastructure.Authentication;

public interface IRefreshTokenService
{
    Task<RefreshToken> GenerateRefreshTokenAsync(ClubManagementDbContext tenantContext, Guid userId, string ipAddress);
    Task<RefreshToken?> GetRefreshTokenAsync(ClubManagementDbContext tenantContext, string token);
    Task<bool> ValidateRefreshTokenAsync(ClubManagementDbContext tenantContext, string token);
    Task RevokeRefreshTokenAsync(ClubManagementDbContext tenantContext, string token, string ipAddress, string reason = "Manual revocation");
    Task RevokeUserRefreshTokensAsync(ClubManagementDbContext tenantContext, Guid userId, string ipAddress, string reason = "User logout");
    Task<RefreshToken> RotateRefreshTokenAsync(ClubManagementDbContext tenantContext, RefreshToken oldToken, string ipAddress);
    Task CleanupExpiredTokensAsync(ClubManagementDbContext tenantContext);
}

public class RefreshTokenService : IRefreshTokenService
{
    private readonly IConfiguration _configuration;
    private readonly int _refreshTokenExpirationDays;

    public RefreshTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
        _refreshTokenExpirationDays = int.Parse(configuration["Jwt:RefreshTokenExpirationDays"] ?? "7");
    }

    public async Task<RefreshToken> GenerateRefreshTokenAsync(ClubManagementDbContext tenantContext, Guid userId, string ipAddress)
    {
        var refreshToken = new RefreshToken
        {
            Token = GenerateRandomToken(),
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays),
            CreatedByIp = ipAddress
        };

        tenantContext.RefreshTokens.Add(refreshToken);
        await tenantContext.SaveChangesAsync();

        return refreshToken;
    }

    public async Task<RefreshToken?> GetRefreshTokenAsync(ClubManagementDbContext tenantContext, string token)
    {
        return await tenantContext.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == token);
    }

    public async Task<bool> ValidateRefreshTokenAsync(ClubManagementDbContext tenantContext, string token)
    {
        var refreshToken = await GetRefreshTokenAsync(tenantContext, token);
        return refreshToken?.IsActive == true;
    }

    public async Task RevokeRefreshTokenAsync(ClubManagementDbContext tenantContext, string token, string ipAddress, string reason = "Manual revocation")
    {
        var refreshToken = await GetRefreshTokenAsync(tenantContext, token);
        if (refreshToken != null && refreshToken.IsActive)
        {
            refreshToken.IsRevoked = true;
            refreshToken.RevokedAt = DateTime.UtcNow;
            refreshToken.RevokedByIp = ipAddress;
            refreshToken.RevokedReason = reason;

            await tenantContext.SaveChangesAsync();
        }
    }

    public async Task RevokeUserRefreshTokensAsync(ClubManagementDbContext tenantContext, Guid userId, string ipAddress, string reason = "User logout")
    {
        var activeTokens = await tenantContext.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpiresAt >= DateTime.UtcNow)
            .ToListAsync();

        foreach (var token in activeTokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = ipAddress;
            token.RevokedReason = reason;
        }

        if (activeTokens.Any())
        {
            await tenantContext.SaveChangesAsync();
        }
    }

    public async Task<RefreshToken> RotateRefreshTokenAsync(ClubManagementDbContext tenantContext, RefreshToken oldToken, string ipAddress)
    {
        // Revoke the old token
        oldToken.IsRevoked = true;
        oldToken.RevokedAt = DateTime.UtcNow;
        oldToken.RevokedByIp = ipAddress;
        oldToken.RevokedReason = "Token rotation";

        // Generate new token
        var newToken = new RefreshToken
        {
            Token = GenerateRandomToken(),
            UserId = oldToken.UserId,
            ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays),
            CreatedByIp = ipAddress
        };

        // Link the tokens
        oldToken.ReplacedByToken = newToken.Token;

        tenantContext.RefreshTokens.Add(newToken);
        await tenantContext.SaveChangesAsync();

        return newToken;
    }

    public async Task CleanupExpiredTokensAsync(ClubManagementDbContext tenantContext)
    {
        var expiredTokens = await tenantContext.RefreshTokens
            .Where(rt => rt.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();

        if (expiredTokens.Any())
        {
            tenantContext.RefreshTokens.RemoveRange(expiredTokens);
            await tenantContext.SaveChangesAsync();
        }
    }

    private static string GenerateRandomToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}