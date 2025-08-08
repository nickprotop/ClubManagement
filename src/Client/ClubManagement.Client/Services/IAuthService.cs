using ClubManagement.Shared.DTOs;

namespace ClubManagement.Client.Services;

public interface IAuthService
{
    Task<ApiResponse<LoginResponse>?> LoginAsync(LoginRequest request);
    Task<ApiResponse<LoginResponse>?> RegisterAsync(RegisterRequest request);
    Task LogoutAsync();
    Task<bool> IsAuthenticatedAsync();
    Task<UserProfileDto?> GetCurrentUserAsync();
    Task<Guid?> GetCurrentMemberIdAsync();
    Task<bool> RefreshTokenAsync();
    Task<bool> ValidateAndRefreshTokenAsync();
    Task HandleSessionExpiredAsync();
    Task SetTokenAsync(string token);
    event Action<bool> AuthenticationStateChanged;
    event Action? SessionExpired;
}