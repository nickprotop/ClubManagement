using ClubManagement.Domain.Entities;
using ClubManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

namespace ClubManagement.Infrastructure.Authentication;

public interface IRefreshTokenService
{
    Task<RefreshToken> GenerateRefreshTokenAsync(Guid userId, string ipAddress);
    Task<RefreshToken?> GetRefreshTokenAsync(string token);
    Task<bool> ValidateRefreshTokenAsync(string token);
    Task RevokeRefreshTokenAsync(string token, string ipAddress, string reason = "Manual revocation");
    Task RevokeUserRefreshTokensAsync(Guid userId, string ipAddress, string reason = "User logout");
    Task<RefreshToken> RotateRefreshTokenAsync(RefreshToken oldToken, string ipAddress);
    Task CleanupExpiredTokensAsync();
}

public class RefreshTokenService : IRefreshTokenService
{
    private readonly ClubManagementDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly int _refreshTokenExpirationDays;

    public RefreshTokenService(ClubManagementDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
        _refreshTokenExpirationDays = int.Parse(configuration["Jwt:RefreshTokenExpirationDays"] ?? "7");
    }

    public async Task<RefreshToken> GenerateRefreshTokenAsync(Guid userId, string ipAddress)
    {
        var refreshToken = new RefreshToken
        {
            Token = GenerateRandomToken(),
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays),
            CreatedByIp = ipAddress
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return refreshToken;
    }

    public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
    {
        return await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == token);
    }

    public async Task<bool> ValidateRefreshTokenAsync(string token)
    {
        var refreshToken = await GetRefreshTokenAsync(token);
        return refreshToken?.IsActive == true;
    }

    public async Task RevokeRefreshTokenAsync(string token, string ipAddress, string reason = "Manual revocation")
    {
        var refreshToken = await GetRefreshTokenAsync(token);
        if (refreshToken != null && refreshToken.IsActive)
        {
            refreshToken.IsRevoked = true;
            refreshToken.RevokedAt = DateTime.UtcNow;
            refreshToken.RevokedByIp = ipAddress;
            refreshToken.RevokedReason = reason;

            await _context.SaveChangesAsync();
        }
    }

    public async Task RevokeUserRefreshTokensAsync(Guid userId, string ipAddress, string reason = "User logout")
    {
        var activeTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked && !rt.IsExpired)
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
            await _context.SaveChangesAsync();
        }
    }

    public async Task<RefreshToken> RotateRefreshTokenAsync(RefreshToken oldToken, string ipAddress)
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

        _context.RefreshTokens.Add(newToken);
        await _context.SaveChangesAsync();

        return newToken;
    }

    public async Task CleanupExpiredTokensAsync()
    {
        var expiredTokens = await _context.RefreshTokens
            .Where(rt => rt.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();

        if (expiredTokens.Any())
        {
            _context.RefreshTokens.RemoveRange(expiredTokens);
            await _context.SaveChangesAsync();
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