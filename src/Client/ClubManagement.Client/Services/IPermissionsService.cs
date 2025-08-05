using ClubManagement.Shared.DTOs;

namespace ClubManagement.Client.Services;

public interface IPermissionsService
{
    Task<ApiResponse<GlobalPermissionsDto>> GetGlobalPermissionsAsync();
    Task<GlobalPermissionsDto?> GetCachedPermissionsAsync();
    Task RefreshPermissionsAsync();
    void ClearCache();
}