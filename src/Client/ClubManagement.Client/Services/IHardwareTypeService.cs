using ClubManagement.Shared.DTOs;
using ClubManagement.Shared.Models;
using ClubManagement.Shared.Models.Authorization;

namespace ClubManagement.Client.Services;

public interface IHardwareTypeService
{
    Task<ApiResponse<List<HardwareTypeDto>>> GetHardwareTypesAsync();
    Task<ApiResponse<HardwareTypeDto>> GetHardwareTypeByIdAsync(Guid id);
    Task<ApiResponse<HardwareTypeDto>> CreateHardwareTypeAsync(CreateHardwareTypeRequest request);
    Task<ApiResponse<HardwareTypeDto>> UpdateHardwareTypeAsync(Guid id, UpdateHardwareTypeRequest request);
    Task<ApiResponse<object>> DeleteHardwareTypeAsync(Guid id);
    Task<ApiResponse<HardwarePermissions>> GetHardwareTypePermissionsAsync(Guid? id = null);
    Task<ApiResponse<List<HardwareTypeDto>>> SearchHardwareTypesAsync(string? searchTerm = null);
}