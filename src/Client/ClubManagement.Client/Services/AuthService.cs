using Microsoft.JSInterop;
using System.Net.Http.Json;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using ClubManagement.Shared.DTOs;
using ClubManagement.Shared.Models;
using Microsoft.Extensions.DependencyInjection;

namespace ClubManagement.Client.Services;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private readonly IServiceProvider _serviceProvider;
    private UserProfileDto? _currentUser;
    private bool _isAuthenticated = false;
    private bool _isRefreshing = false;
    private DateTime? _tokenExpiresAt;

    public event Action<bool>? AuthenticationStateChanged;
    public event Action? SessionExpired;

    public AuthService(HttpClient httpClient, IJSRuntime jsRuntime, IServiceProvider serviceProvider)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
        _serviceProvider = serviceProvider;
    }

    public async Task<ApiResponse<LoginResponse>?> LoginAsync(LoginRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();

            if (result?.Success == true && result.Data != null)
            {
                await SetAuthenticationDataAsync(result.Data);
            }

            return result;
        }
        catch (Exception ex)
        {
            return ApiResponse<LoginResponse>.ErrorResult($"Login failed: {ex.Message}");
        }
    }

    public async Task<ApiResponse<LoginResponse>?> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/register", request);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();

            if (result?.Success == true && result.Data != null)
            {
                await SetAuthenticationDataAsync(result.Data);
            }

            return result;
        }
        catch (Exception ex)
        {
            return ApiResponse<LoginResponse>.ErrorResult($"Registration failed: {ex.Message}");
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            // Call logout endpoint to revoke refresh token
            await CallLogoutEndpointAsync();
        }
        catch
        {
            // Continue with local logout even if server call fails
        }

        await ClearAuthenticationDataAsync();
    }

    private async Task CallLogoutEndpointAsync()
    {
        var refreshToken = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "refreshToken");
        if (!string.IsNullOrEmpty(refreshToken))
        {
            var request = new RefreshTokenRequest { RefreshToken = refreshToken };
            await _httpClient.PostAsJsonAsync("api/auth/logout", request);
        }
    }

    private async Task ClearAuthenticationDataAsync()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "refreshToken");
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "currentUser");
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "tokenExpiresAt");

        _currentUser = null;
        _isAuthenticated = false;
        _tokenExpiresAt = null;
        _httpClient.DefaultRequestHeaders.Authorization = null;

        AuthenticationStateChanged?.Invoke(false);
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        try
        {
            if (_isAuthenticated && !IsTokenExpiringSoon())
                return true;

            return await ValidateAndRefreshTokenAsync();
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ValidateAndRefreshTokenAsync()
    {
        try
        {
            var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");
            if (string.IsNullOrEmpty(token))
                return false;

            // Parse token to check expiration
            var tokenExpiration = GetTokenExpiration(token);
            if (tokenExpiration == null)
                return false;

            _tokenExpiresAt = tokenExpiration;

            // If token is expired or expiring soon, try to refresh
            if (IsTokenExpired() || IsTokenExpiringSoon())
            {
                return await RefreshTokenAsync();
            }

            // Token is still valid, restore authentication state
            var userJson = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "currentUser");
            if (!string.IsNullOrEmpty(userJson))
            {
                _currentUser = JsonSerializer.Deserialize<UserProfileDto>(userJson);
                _isAuthenticated = true;
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            return _isAuthenticated;
        }
        catch
        {
            // Don't clear auth data on validation errors during startup
            return false;
        }
    }

    public async Task<UserProfileDto?> GetCurrentUserAsync()
    {
        if (_currentUser != null)
            return _currentUser;

        await IsAuthenticatedAsync();
        return _currentUser;
    }

    public async Task<bool> RefreshTokenAsync()
    {
        if (_isRefreshing)
            return _isAuthenticated;

        _isRefreshing = true;

        try
        {
            var refreshToken = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "refreshToken");
            if (string.IsNullOrEmpty(refreshToken))
            {
                return false;
            }

            var request = new RefreshTokenRequest { RefreshToken = refreshToken };
            var response = await _httpClient.PostAsJsonAsync("api/auth/refresh", request);
            
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<RefreshTokenResponse>>();
            if (result?.Success != true || result.Data == null)
            {
                return false;
            }

            // Update tokens
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "authToken", result.Data.AccessToken);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "refreshToken", result.Data.RefreshToken);

            _tokenExpiresAt = result.Data.ExpiresAt;
            _isAuthenticated = true;
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", result.Data.AccessToken);

            // Preload permissions after token refresh
            try
            {
                var permissionsService = _serviceProvider.GetService<IPermissionsService>();
                if (permissionsService != null)
                {
                    _ = Task.Run(() => permissionsService.PreloadPermissionsAsync()); // Fire and forget
                }
            }
            catch
            {
                // Ignore preload errors - permissions will be loaded on demand
            }

            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    public async Task HandleSessionExpiredAsync()
    {
        await ClearAuthenticationDataAsync();
        SessionExpired?.Invoke();
    }

    private DateTime? GetTokenExpiration(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jsonToken = tokenHandler.ReadJwtToken(token);
            return jsonToken.ValidTo;
        }
        catch
        {
            return null;
        }
    }

    private bool IsTokenExpired()
    {
        return _tokenExpiresAt.HasValue && DateTime.UtcNow >= _tokenExpiresAt.Value;
    }

    private bool IsTokenExpiringSoon()
    {
        // Refresh token if it expires within 5 minutes
        return _tokenExpiresAt.HasValue && DateTime.UtcNow >= _tokenExpiresAt.Value.AddMinutes(-5);
    }

    private async Task SetAuthenticationDataAsync(LoginResponse loginResponse)
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "authToken", loginResponse.AccessToken);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "refreshToken", loginResponse.RefreshToken);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "currentUser", JsonSerializer.Serialize(loginResponse.User));

        _tokenExpiresAt = loginResponse.ExpiresAt;
        _currentUser = loginResponse.User;
        _isAuthenticated = true;
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResponse.AccessToken);

        // Preload permissions after successful authentication
        try
        {
            var permissionsService = _serviceProvider.GetService<IPermissionsService>();
            if (permissionsService != null)
            {
                _ = Task.Run(() => permissionsService.PreloadPermissionsAsync()); // Fire and forget
            }
        }
        catch
        {
            // Ignore preload errors - permissions will be loaded on demand
        }

        AuthenticationStateChanged?.Invoke(true);
    }

    public async Task SetTokenAsync(string token)
    {
        // Parse the token to get expiration and user data
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadJwtToken(token);
        
        // Extract user information from token claims
        var userIdClaim = jsonToken.Claims.FirstOrDefault(x => x.Type == "sub")?.Value;
        var usernameClaim = jsonToken.Claims.FirstOrDefault(x => x.Type == "email")?.Value;
        var roleClaim = jsonToken.Claims.FirstOrDefault(x => x.Type == "role")?.Value;
        
        if (userIdClaim != null && usernameClaim != null)
        {
            var user = new UserProfileDto
            {
                Id = Guid.Parse(userIdClaim),
                Email = usernameClaim,
                Role = Enum.TryParse<UserRole>(roleClaim, out var role) ? role : UserRole.Member
            };

            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "authToken", token);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "currentUser", JsonSerializer.Serialize(user));

            _tokenExpiresAt = jsonToken.ValidTo;
            _currentUser = user;
            _isAuthenticated = true;
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            AuthenticationStateChanged?.Invoke(true);
        }
    }
}