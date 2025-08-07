using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClubManagement.Infrastructure.Data;
using ClubManagement.Infrastructure.Services;
using ClubManagement.Infrastructure.Authentication;
using ClubManagement.Shared.DTOs;
using ClubManagement.Shared.Models;
using ClubManagement.Domain.Entities;
using System.Security.Claims;

namespace ClubManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ITenantDbContextFactory _tenantDbContextFactory;
    private readonly ITenantService _tenantService;
    private readonly IJwtService _jwtService;
    private readonly IPasswordService _passwordService;
    private readonly IRefreshTokenService _refreshTokenService;

    public AuthController(
        ITenantDbContextFactory tenantDbContextFactory,
        ITenantService tenantService,
        IJwtService jwtService,
        IPasswordService passwordService,
        IRefreshTokenService refreshTokenService)
    {
        _tenantDbContextFactory = tenantDbContextFactory;
        _tenantService = tenantService;
        _jwtService = jwtService;
        _passwordService = passwordService;
        _refreshTokenService = refreshTokenService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrEmpty(request.TenantDomain))
        {
            return BadRequest(ApiResponse<LoginResponse>.ErrorResult("Tenant domain is required"));
        }

        var tenant = await _tenantService.GetTenantByDomainAsync(request.TenantDomain);
        if (tenant == null)
        {
            return BadRequest(ApiResponse<LoginResponse>.ErrorResult("Invalid tenant domain"));
        }

        // Get tenant database context
        using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

        var user = await tenantContext.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.TenantId == tenant.Id);

        if (user == null)
        {
            return BadRequest(ApiResponse<LoginResponse>.ErrorResult("Invalid email or password"));
        }

        // Verify password
        if (!_passwordService.VerifyPassword(request.Password, user.PasswordHash, user.PasswordSalt))
        {
            return BadRequest(ApiResponse<LoginResponse>.ErrorResult("Invalid email or password"));
        }

        if (user.Status != UserStatus.Active)
        {
            return BadRequest(ApiResponse<LoginResponse>.ErrorResult("Account is not active"));
        }

        var accessToken = _jwtService.GenerateAccessToken(user, tenant.Id);
        
        // Generate and store refresh token
        var ipAddress = GetIpAddress();
        var refreshTokenEntity = await _refreshTokenService.GenerateRefreshTokenAsync(tenantContext, user.Id, ipAddress);

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await tenantContext.SaveChangesAsync();

        var userProfile = new UserProfileDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role,
            Status = user.Status,
            ProfilePhotoUrl = user.ProfilePhotoUrl,
            LastLoginAt = user.LastLoginAt,
            EmailVerified = user.EmailVerified,
            CustomFields = user.CustomFields
        };

        var loginResponse = new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenEntity.Token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            User = userProfile
        };

        return Ok(ApiResponse<LoginResponse>.SuccessResult(loginResponse, "Login successful"));
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Register([FromBody] RegisterRequest request)
    {
        var tenant = await _tenantService.GetTenantByDomainAsync(request.TenantDomain);
        if (tenant == null)
        {
            return BadRequest(ApiResponse<LoginResponse>.ErrorResult("Invalid tenant domain"));
        }

        using var tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenant.Domain);

        var existingUser = await tenantContext.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.TenantId == tenant.Id);

        if (existingUser != null)
        {
            return BadRequest(ApiResponse<LoginResponse>.ErrorResult("User with this email already exists"));
        }

        // Hash the password
        var (passwordHash, passwordSalt) = _passwordService.HashPasswordWithSeparateSalt(request.Password);

        var user = new User
        {
            Email = request.Email,
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            Role = UserRole.Member,
            Status = UserStatus.Active,
            TenantId = tenant.Id,
            EmailVerified = false,
            PasswordChangedAt = DateTime.UtcNow
        };

        tenantContext.Users.Add(user);
        await tenantContext.SaveChangesAsync();

        var accessToken = _jwtService.GenerateAccessToken(user, tenant.Id);
        
        // Generate and store refresh token
        var ipAddress = GetIpAddress();
        var refreshTokenEntity = await _refreshTokenService.GenerateRefreshTokenAsync(tenantContext, user.Id, ipAddress);

        var userProfile = new UserProfileDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role,
            Status = user.Status,
            ProfilePhotoUrl = user.ProfilePhotoUrl,
            LastLoginAt = user.LastLoginAt,
            EmailVerified = user.EmailVerified,
            CustomFields = user.CustomFields
        };

        var loginResponse = new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenEntity.Token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            User = userProfile
        };

        return Ok(ApiResponse<LoginResponse>.SuccessResult(loginResponse, "Registration successful"));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<RefreshTokenResponse>>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
        {
            return BadRequest(ApiResponse<RefreshTokenResponse>.ErrorResult("Refresh token is required"));
        }

        // Get tenant from request (we need it to create tenant context)
        // For now, we'll extract it from the existing refresh token's user data
        // This is a temporary approach - in production you might want to store tenant info in the refresh token
        
        // First, we need to find which tenant this refresh token belongs to
        // We'll search across all tenants - this is not ideal but necessary for this migration
        var allTenants = await _tenantService.GetAllTenantsAsync();
        RefreshToken? refreshToken = null;
        ClubManagementDbContext? tenantContext = null;
        
        foreach (var tenantItem in allTenants)
        {
            try
            {
                using var tempContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenantItem.Domain);
                var tempToken = await _refreshTokenService.GetRefreshTokenAsync(tempContext, request.RefreshToken);
                if (tempToken != null)
                {
                    refreshToken = tempToken;
                    tenantContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenantItem.Domain);
                    break;
                }
            }
            catch
            {
                // Continue searching other tenants
                continue;
            }
        }
        
        if (refreshToken == null || tenantContext == null || !refreshToken.IsActive)
        {
            return BadRequest(ApiResponse<RefreshTokenResponse>.ErrorResult("Invalid or expired refresh token"));
        }

        var user = refreshToken.User;
        if (user.Status != UserStatus.Active)
        {
            tenantContext.Dispose();
            return BadRequest(ApiResponse<RefreshTokenResponse>.ErrorResult("Account is not active"));
        }

        // Get tenant for token generation
        var tenant = await _tenantService.GetTenantByIdAsync(user.TenantId);
        if (tenant == null)
        {
            tenantContext.Dispose();
            return BadRequest(ApiResponse<RefreshTokenResponse>.ErrorResult("Invalid tenant"));
        }

        // Generate new tokens
        var newAccessToken = _jwtService.GenerateAccessToken(user, tenant.Id);
        var ipAddress = GetIpAddress();
        var newRefreshToken = await _refreshTokenService.RotateRefreshTokenAsync(tenantContext, refreshToken, ipAddress);
        
        // Dispose the tenant context
        tenantContext.Dispose();

        var response = new RefreshTokenResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken.Token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60)
        };

        return Ok(ApiResponse<RefreshTokenResponse>.SuccessResult(response, "Token refreshed successfully"));
    }

    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse<object>>> Logout([FromBody] RefreshTokenRequest request)
    {
        if (!string.IsNullOrEmpty(request.RefreshToken))
        {
            var ipAddress = GetIpAddress();
            // Similar approach - find the tenant context for this refresh token
            var allTenants = await _tenantService.GetAllTenantsAsync();
            foreach (var tenantItem in allTenants)
            {
                try
                {
                    using var tempContext = await _tenantDbContextFactory.CreateTenantDbContextAsync(tenantItem.Domain);
                    var tempToken = await _refreshTokenService.GetRefreshTokenAsync(tempContext, request.RefreshToken);
                    if (tempToken != null)
                    {
                        await _refreshTokenService.RevokeRefreshTokenAsync(tempContext, request.RefreshToken, ipAddress, "User logout");
                        break;
                    }
                }
                catch
                {
                    // Continue searching other tenants
                    continue;
                }
            }
        }

        return Ok(ApiResponse<object>.SuccessResult(new object(), "Logout successful"));
    }

    private string GetIpAddress()
    {
        // Try to get the real IP address from various headers
        if (Request.Headers.ContainsKey("X-Forwarded-For"))
        {
            var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',').FirstOrDefault()?.Trim() ?? "Unknown";
            }
        }

        if (Request.Headers.ContainsKey("X-Real-IP"))
        {
            var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }
        }

        // Fall back to connection remote IP
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}