using ClubManagement.Shared.DTOs;

namespace ClubManagement.Client.Services;

public interface IAuthService
{
    Task<ApiResponse<LoginResponse>?> LoginAsync(LoginRequest request);
    Task<ApiResponse<LoginResponse>?> RegisterAsync(RegisterRequest request);
    Task LogoutAsync();
    Task<bool> IsAuthenticatedAsync();
    Task<UserProfileDto?> GetCurrentUserAsync();
    Task<bool> RefreshTokenAsync();
    Task<bool> ValidateAndRefreshTokenAsync();
    Task HandleSessionExpiredAsync();
    event Action<bool> AuthenticationStateChanged;
    event Action? SessionExpired;
}