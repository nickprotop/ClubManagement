using Microsoft.JSInterop;
using System.Net.Http.Json;
using System.Text.Json;
using ClubManagement.Shared.DTOs;

namespace ClubManagement.Client.Services;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private UserProfileDto? _currentUser;
    private bool _isAuthenticated = false;

    public event Action<bool>? AuthenticationStateChanged;

    public AuthService(HttpClient httpClient, IJSRuntime jsRuntime)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
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
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "refreshToken");
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "currentUser");

        _currentUser = null;
        _isAuthenticated = false;
        _httpClient.DefaultRequestHeaders.Authorization = null;

        AuthenticationStateChanged?.Invoke(false);
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        if (_isAuthenticated)
            return true;

        try
        {
            var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");
            if (string.IsNullOrEmpty(token))
                return false;

            // TODO: Validate token expiration
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

    private async Task SetAuthenticationDataAsync(LoginResponse loginResponse)
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "authToken", loginResponse.AccessToken);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "refreshToken", loginResponse.RefreshToken);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "currentUser", JsonSerializer.Serialize(loginResponse.User));

        _currentUser = loginResponse.User;
        _isAuthenticated = true;
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResponse.AccessToken);

        AuthenticationStateChanged?.Invoke(true);
    }
}