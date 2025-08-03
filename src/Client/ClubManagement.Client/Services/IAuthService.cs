using ClubManagement.Shared.DTOs;

namespace ClubManagement.Client.Services;

public interface IAuthService
{
    Task<ApiResponse<LoginResponse>?> LoginAsync(LoginRequest request);
    Task<ApiResponse<LoginResponse>?> RegisterAsync(RegisterRequest request);
    Task LogoutAsync();
    Task<bool> IsAuthenticatedAsync();
    Task<UserProfileDto?> GetCurrentUserAsync();
    event Action<bool> AuthenticationStateChanged;
}