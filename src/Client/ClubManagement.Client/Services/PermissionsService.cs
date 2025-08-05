using ClubManagement.Shared.DTOs;

namespace ClubManagement.Client.Services;

public class PermissionsService : IPermissionsService
{
    private readonly IApiService _apiService;
    private GlobalPermissionsDto? _cachedPermissions;
    private DateTime? _cacheExpiry;
    private readonly TimeSpan _cacheTimeout = TimeSpan.FromMinutes(15); // Cache for 15 minutes

    public PermissionsService(IApiService apiService)
    {
        _apiService = apiService;
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

    public void ClearCache()
    {
        _cachedPermissions = null;
        _cacheExpiry = null;
    }
}