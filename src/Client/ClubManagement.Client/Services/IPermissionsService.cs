using ClubManagement.Shared.DTOs;

namespace ClubManagement.Client.Services;

public interface IPermissionsService
{
    Task<ApiResponse<GlobalPermissionsDto>> GetGlobalPermissionsAsync();
    Task<GlobalPermissionsDto?> GetCachedPermissionsAsync();
    Task RefreshPermissionsAsync();
    Task PreloadPermissionsAsync(); // Preload permissions without returning them
    void ClearCache();
}