using ClubManagement.Shared.DTOs;
using ClubManagement.Shared.Models;
using ClubManagement.Shared.Models.Authorization;
using System.Text;
using System.Text.Json;

namespace ClubManagement.Client.Services;

public class HardwareService : IHardwareService
{
    private readonly IApiService _apiService;

    public HardwareService(IApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<ApiResponse<List<HardwareDto>>> GetHardwareAsync()
    {
        var response = await _apiService.GetAsync<PagedResult<HardwareDto>>("api/hardware");
        if (response.Success && response.Data != null)
        {
            return ApiResponse<List<HardwareDto>>.SuccessResult(response.Data.Items);
        }
        return ApiResponse<List<HardwareDto>>.ErrorResult(response.Message ?? "Failed to retrieve hardware");
    }

    public async Task<ApiResponse<HardwareDto>> GetHardwareByIdAsync(Guid id)
    {
        return await _apiService.GetAsync<HardwareDto>($"api/hardware/{id}");
    }

    public async Task<ApiResponse<HardwareDto>> CreateHardwareAsync(CreateHardwareRequest request)
    {
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _apiService.PostAsync<HardwareDto>("api/hardware", content);
    }

    public async Task<ApiResponse<HardwareDto>> UpdateHardwareAsync(Guid id, UpdateHardwareRequest request)
    {
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _apiService.PutAsync<HardwareDto>($"api/hardware/{id}", content);
    }

    public async Task<ApiResponse<object>> DeleteHardwareAsync(Guid id)
    {
        return await _apiService.DeleteAsync<object>($"api/hardware/{id}");
    }

    public async Task<ApiResponse<HardwarePermissions>> GetHardwarePermissionsAsync(Guid? id = null)
    {
        var url = id.HasValue ? $"api/hardware/{id}/permissions" : "api/hardware/permissions";
        return await _apiService.GetAsync<HardwarePermissions>(url);
    }

    public async Task<ApiResponse<List<HardwareDto>>> SearchHardwareAsync(string? searchTerm = null, Guid? hardwareTypeId = null, HardwareStatus? status = null)
    {
        var queryParams = new List<string>();
        
        if (!string.IsNullOrEmpty(searchTerm))
            queryParams.Add($"searchTerm={Uri.EscapeDataString(searchTerm)}");
            
        if (hardwareTypeId.HasValue)
            queryParams.Add($"hardwareTypeId={hardwareTypeId}");
            
        if (status.HasValue)
            queryParams.Add($"status={status}");

        var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
        return await _apiService.GetAsync<List<HardwareDto>>($"api/hardware/search{queryString}");
    }

    public async Task<ApiResponse<List<HardwareDto>>> GetHardwareByTypeAsync(Guid hardwareTypeId)
    {
        return await _apiService.GetAsync<List<HardwareDto>>($"api/hardware/type/{hardwareTypeId}");
    }

    public async Task<ApiResponse<List<HardwareUsageHistoryDto>>> GetHardwareUsageHistoryAsync(Guid hardwareId)
    {
        return await _apiService.GetAsync<List<HardwareUsageHistoryDto>>($"api/hardware/{hardwareId}/usage-history");
    }
}