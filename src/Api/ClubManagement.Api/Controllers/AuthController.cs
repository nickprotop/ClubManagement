using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClubManagement.Infrastructure.Data;
using ClubManagement.Infrastructure.Services;
using ClubManagement.Infrastructure.Authentication;
using ClubManagement.Shared.DTOs;
using ClubManagement.Shared.Models;

namespace ClubManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ClubManagementDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IJwtService _jwtService;
    private readonly IPasswordService _passwordService;

    public AuthController(
        ClubManagementDbContext context,
        ITenantService tenantService,
        IJwtService jwtService,
        IPasswordService passwordService)
    {
        _context = context;
        _tenantService = tenantService;
        _jwtService = jwtService;
        _passwordService = passwordService;
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

        // Switch to tenant schema
        await _context.Database.ExecuteSqlRawAsync($"SET search_path TO \"{tenant.SchemaName}\"");

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.TenantId == tenant.Id);

        if (user == null)
        {
            return BadRequest(ApiResponse<LoginResponse>.ErrorResult("Invalid email or password"));
        }

        // For now, we need to add password field to User model or create separate UserCredentials table
        // TODO: Implement proper password verification
        // if (!_passwordService.VerifyPassword(request.Password, user.PasswordHash))
        // {
        //     return BadRequest(ApiResponse<LoginResponse>.ErrorResult("Invalid email or password"));
        // }

        if (user.Status != UserStatus.Active)
        {
            return BadRequest(ApiResponse<LoginResponse>.ErrorResult("Account is not active"));
        }

        var accessToken = _jwtService.GenerateAccessToken(user, tenant.Id);
        var refreshToken = _jwtService.GenerateRefreshToken();

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

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
            RefreshToken = refreshToken,
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

        // Switch to tenant schema
        await _context.Database.ExecuteSqlRawAsync($"SET search_path TO \"{tenant.SchemaName}\"");

        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.TenantId == tenant.Id);

        if (existingUser != null)
        {
            return BadRequest(ApiResponse<LoginResponse>.ErrorResult("User with this email already exists"));
        }

        var user = new User
        {
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            Role = UserRole.Member,
            Status = UserStatus.Active,
            TenantId = tenant.Id,
            EmailVerified = false
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var accessToken = _jwtService.GenerateAccessToken(user, tenant.Id);
        var refreshToken = _jwtService.GenerateRefreshToken();

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
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            User = userProfile
        };

        return Ok(ApiResponse<LoginResponse>.SuccessResult(loginResponse, "Registration successful"));
    }
}