using ClubManagement.Shared.DTOs;
using ClubManagement.Shared.Models;
using ClubManagement.Shared.Models.Authorization;

namespace ClubManagement.Client.Services;

public interface IHardwareService
{
    Task<ApiResponse<List<HardwareDto>>> GetHardwareAsync();
    Task<ApiResponse<HardwareDto>> GetHardwareByIdAsync(Guid id);
    Task<ApiResponse<HardwareDto>> CreateHardwareAsync(CreateHardwareRequest request);
    Task<ApiResponse<HardwareDto>> UpdateHardwareAsync(Guid id, UpdateHardwareRequest request);
    Task<ApiResponse<object>> DeleteHardwareAsync(Guid id);
    Task<ApiResponse<HardwarePermissions>> GetHardwarePermissionsAsync(Guid? id = null);
    Task<ApiResponse<List<HardwareDto>>> SearchHardwareAsync(string? searchTerm = null, Guid? hardwareTypeId = null, HardwareStatus? status = null);
    Task<ApiResponse<List<HardwareDto>>> GetHardwareByTypeAsync(Guid hardwareTypeId);
    Task<ApiResponse<List<HardwareUsageHistoryDto>>> GetHardwareUsageHistoryAsync(Guid hardwareId);
}