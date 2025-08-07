using ClubManagement.Shared.DTOs;
using ClubManagement.Shared.Models;
using ClubManagement.Shared.Models.Authorization;
using System.Text;
using System.Text.Json;

namespace ClubManagement.Client.Services;

public class HardwareTypeService : IHardwareTypeService
{
    private readonly IApiService _apiService;

    public HardwareTypeService(IApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<ApiResponse<List<HardwareTypeDto>>> GetHardwareTypesAsync()
    {
        return await _apiService.GetAsync<List<HardwareTypeDto>>("api/hardware-types");
    }

    public async Task<ApiResponse<HardwareTypeDto>> GetHardwareTypeByIdAsync(Guid id)
    {
        return await _apiService.GetAsync<HardwareTypeDto>($"api/hardware-types/{id}");
    }

    public async Task<ApiResponse<HardwareTypeDto>> CreateHardwareTypeAsync(CreateHardwareTypeRequest request)
    {
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _apiService.PostAsync<HardwareTypeDto>("api/hardware-types", content);
    }

    public async Task<ApiResponse<HardwareTypeDto>> UpdateHardwareTypeAsync(Guid id, UpdateHardwareTypeRequest request)
    {
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _apiService.PutAsync<HardwareTypeDto>($"api/hardware-types/{id}", content);
    }

    public async Task<ApiResponse<object>> DeleteHardwareTypeAsync(Guid id)
    {
        return await _apiService.DeleteAsync<object>($"api/hardware-types/{id}");
    }

    public async Task<ApiResponse<HardwarePermissions>> GetHardwareTypePermissionsAsync(Guid? id = null)
    {
        var url = id.HasValue ? $"api/hardware-types/{id}/permissions" : "api/hardware-types/permissions";
        return await _apiService.GetAsync<HardwarePermissions>(url);
    }

    public async Task<ApiResponse<List<HardwareTypeDto>>> SearchHardwareTypesAsync(string? searchTerm = null)
    {
        var queryString = !string.IsNullOrEmpty(searchTerm) ? $"?searchTerm={Uri.EscapeDataString(searchTerm)}" : "";
        return await _apiService.GetAsync<List<HardwareTypeDto>>($"api/hardware-types/search{queryString}");
    }
}