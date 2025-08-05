using ClubManagement.Shared.DTOs;

namespace ClubManagement.Client.Services;

public class PermissionsService : IPermissionsService, IDisposable
{
    private readonly IApiService _apiService;
    private readonly IAuthService _authService;
    private GlobalPermissionsDto? _cachedPermissions;
    private DateTime? _cacheExpiry;
    private readonly TimeSpan _cacheTimeout = TimeSpan.FromMinutes(15); // Cache for 15 minutes

    public PermissionsService(IApiService apiService, IAuthService authService)
    {
        _apiService = apiService;
        _authService = authService;
        
        // Clear cache on logout only
        _authService.AuthenticationStateChanged += OnAuthenticationStateChanged;
    }
    
    private void OnAuthenticationStateChanged(bool isAuthenticated)
    {
        // Clear cache only when logging out (not logging in)
        if (!isAuthenticated)
        {
            ClearCache();
        }
    }

    public async Task<ApiResponse<GlobalPermissionsDto>> GetGlobalPermissionsAsync()
    {
        var response = await _apiService.GetAsync<GlobalPermissionsDto>("/api/permissions");
        
        if (response.Success && response.Data != null)
        {
            _cachedPermissions = response.Data;
            _cacheExpiry = DateTime.UtcNow.Add(_cacheTimeout);
        }
        
        return response;
    }

    public async Task<GlobalPermissionsDto?> GetCachedPermissionsAsync()
    {
        // Return cached permissions if still valid
        if (_cachedPermissions != null && _cacheExpiry.HasValue && DateTime.UtcNow < _cacheExpiry.Value)
        {
            return _cachedPermissions;
        }

        // Cache is expired or empty, fetch fresh permissions
        var response = await GetGlobalPermissionsAsync();
        return response.Success ? response.Data : null;
    }

    public async Task RefreshPermissionsAsync()
    {
        ClearCache();
        await GetGlobalPermissionsAsync();
    }

    public async Task PreloadPermissionsAsync()
    {
        try
        {
            await GetGlobalPermissionsAsync();
        }
        catch
        {
            // Ignore preload errors - permissions will be loaded on demand
        }
    }

    public void ClearCache()
    {
        _cachedPermissions = null;
        _cacheExpiry = null;
    }

    public void Dispose()
    {
        _authService.AuthenticationStateChanged -= OnAuthenticationStateChanged;
    }
}